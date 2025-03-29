import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Stack } from "@fluentui/react";
import {
  Button,
  Image,
  Tree,
  TreeItem,
  TreeItemLayout,
  Spinner,
  Subtitle2,
  Tooltip,
} from "@fluentui/react-components";
import { ChevronDoubleLeft20Regular, ChevronDoubleRight20Regular, ArrowSwap20Regular } from "@fluentui/react-icons";

import { callDatahubOpen, callAuthAcquireAccessToken } from "../../controller/CleanProjectController";
import { FileMetadata } from "../../models/LakehouseExplorerModel";
import "./../../styles.scss";

import { getFilesInLakehouse, getFilesInLakehousePath } from "../../controller/LakehouseExplorerController";
import { ContextProps, CleanProjectPageProps } from "../../App";
import { GenericItem as LakehouseMetadata } from "../..//models/CleanProjectModel";
import { FileTree } from "./FileTree";

export type ExtraProps = {
  title: string;
  selectInstructions: string;
}
export function LakehouseFileExplorerComponent({
  workloadClient,
  title,
  selectFileInstructions,
  selectLakeHouseInstructions,
  isFolderOnly,
  onFileSelected,
}: CleanProjectPageProps) {
  const workloadBackendUrl = process.env.WORKLOAD_BE_URL;
  const [selectedLakehouse, setSelectedLakehouse] = useState<LakehouseMetadata>(null);
  const [filesInLakehouse, setFilesInLakehouse] = useState<FileMetadata[]>(null);
  const [_, setFileSelected] = useState<FileMetadata>(null);
  const [loadingStatus, setLoadingStatus] = useState<string>("idle");
  const [isExplorerVisible, setIsExplorerVisible] = useState<boolean>(true);
  const [isFrontendOnly, setIsFrontendOnly] = useState<boolean>(true);
  const pageContext = useParams<ContextProps>();

  useEffect(() => {
    const fetchFiles = async () => {
      if (selectedLakehouse) {
        setLoadingStatus("loading");
        let success = false;
        try {
          success = await setFiles(null);
        } catch (exception) {
          success = await setFiles(".default");
        }
        setLoadingStatus(success ? "idle" : "error");
      }
    };
    fetchFiles();
  }, [selectedLakehouse]);

  useEffect(() => {
    if (pageContext.itemObjectId) {
      setIsFrontendOnly(false);
    }
  }, []);

  async function setFiles(additionalScopesToConsent: string): Promise<boolean> {
    let accessToken = await callAuthAcquireAccessToken(workloadClient, additionalScopesToConsent);
    const filePath = getFilesInLakehousePath(
      workloadBackendUrl,
      selectedLakehouse.workspaceId,
      selectedLakehouse.id
    );
    let files = await getFilesInLakehouse(filePath, accessToken.token);
    if (files) {
      setFilesInLakehouse(files);
      return true;
    }
    return false;
  }

  async function onDatahubClicked() {
    const result = await callDatahubOpen(
      ["Lakehouse"],
      selectLakeHouseInstructions,
      false,
      workloadClient
    );

    if (!result) {
      return;
    }
    setSelectedLakehouse(result);
    setFileSelected(null);
  }

  function toggleExplorer() {
    setIsExplorerVisible(!isExplorerVisible);
  }

  function fileSelectedCallback(fileSelected: FileMetadata) {
    setFileSelected(fileSelected);
    // setFilesInLakehouse to rerender the tree
    const updatedFiles = filesInLakehouse.map((file: FileMetadata) => {
      const result = { ...file, isSelected: file.path === fileSelected.path && file.name == fileSelected.name };
      if (result.isSelected) {
        onFileSelected(result, selectedLakehouse);
      }

      return result;

    });
    setFilesInLakehouse(updatedFiles);
  }

  return (
    <>
      <Stack className={`explorer ${isExplorerVisible ? "" : "hidden-explorer"}`}>
        <div className={`top ${isExplorerVisible ? "" : "vertical-text"}`}>
          {!isExplorerVisible && (
            <Button onClick={toggleExplorer} appearance="subtle" icon={<ChevronDoubleRight20Regular />}></Button>
          )}
          <h1>{title}</h1>
          {isExplorerVisible && (
            <Button onClick={toggleExplorer} appearance="subtle" icon={<ChevronDoubleLeft20Regular />}></Button>
          )}
        </div>
        {selectedLakehouse == null && isExplorerVisible && (
          <Stack className="main-body" verticalAlign="center" horizontalAlign="center" tokens={{ childrenGap: 5 }}>
            <Image src="../../../internalAssets/Page.svg" />
            <span className="add">{selectFileInstructions}</span>
            <Tooltip content={isFrontendOnly ? "Feature not available in frontend only" : "Open Datahub Explorer"} relationship="label">
              <Button className="add-button" size="small" disabled={isFrontendOnly} onClick={() => onDatahubClicked()} appearance="primary">
                Select
              </Button>
            </Tooltip>
          </Stack>
        )}
        {loadingStatus === "loading" && <Spinner className="main-body" label="Loading Files" />}
        {selectedLakehouse && loadingStatus == "idle" && filesInLakehouse != null && isExplorerVisible && (
          <Tree
            aria-label="Files in Lakehouse"
            className="selector-body"
            size="medium"
            defaultOpenItems={["Lakehouse", "Files"]}
          >
            <div className="tree-container">
              <TreeItem className="selector-tree-item" itemType="branch" value="Lakehouse">
                <Tooltip relationship="label" content={selectedLakehouse?.displayName}>
                  <TreeItemLayout
                    aside={
                      <Button appearance="subtle" icon={<ArrowSwap20Regular />} onClick={onDatahubClicked}></Button>
                    }
                  >
                    {selectedLakehouse?.displayName}
                  </TreeItemLayout>
                </Tooltip>
                <Tree className="tree" selectionMode="single">
                  <FileTree
                    allFilesInLakehouse={filesInLakehouse}
                    onSelectFileCallback={fileSelectedCallback}
                    isFolderOnly={isFolderOnly} />
                </Tree>
              </TreeItem>
            </div>
          </Tree>
        )}
        {loadingStatus === "error" && isExplorerVisible && <div className="main-body">
          <Subtitle2>Error loading files</Subtitle2>
          <p>Do you have permission to view this lakehouse?</p>
        </div>}
      </Stack>
      {/* <Subtitle2>File Selected: {fileSelected?.name}</Subtitle2> */}
    </>
  );
}
