import React, { useState } from "react";
import {
  //Text, 
  useId, Input, Label, 
  Body1Stronger,
  Button,
  } from "@fluentui/react-components";
import { FileSelectionProps } from "src/App";
import "./../../styles.scss";
import { LakehouseFileExplorerComponent } from "../SampleWorkloadFileExplorer/SampleWorkloadFileExplorer";
import { FileMetadata } from "src/models/LakehouseExplorerModel";
import { removePrefix, useStyles } from "./utils";
import { FileRow } from "../../controller/LakehouseExplorerController";
import { GenericItem } from "../../models/CleanProjectModel";
import { FilePreview } from "./FilePreview";

export const SourceFileSelector = (props: FileSelectionProps) => {
  const { workloadClient, onFileSelected } = props;
  const styles = useStyles();
  const selectedFileInputId = useId("input-selectedFile");
  const [selectedFile, setSelectedFile] = useState<FileMetadata>(null);
  const [selectedLakehouse, setSelectedLakehouse] = useState<GenericItem>(null);
  const [fileRows, setFileRows] = useState<FileRow[]>([]);
  
  const selectedItemPath = selectedFile
    ? removePrefix(selectedFile.path + '/' + selectedFile.name)
    : null;
  return (
    <div className={styles.fileSelector}>
      <LakehouseFileExplorerComponent
        workloadClient={workloadClient}
        title={"Input File"}
        selectFileInstructions={"Select the source file"}
        selectLakeHouseInstructions={"Select the lakehouse where the source file is located"}
        isFolderOnly={false}
        onFileSelected={(file, lakehouse) => {
          setSelectedFile(file);
          setSelectedLakehouse(lakehouse);
        }} />
      <div className={styles.fileSelectorInfoPanel}>
        {!selectedItemPath && (<div className={styles.formInputGroup}>
          <Body1Stronger>Please select the source file that you want to clean.</Body1Stronger>
        </div>)}
        {selectedItemPath && (<div className={styles.formInputGroup}>
          <Label size="medium" htmlFor={selectedFileInputId} weight="semibold">
            Source File
          </Label>
          <Input size="medium" id={selectedFileInputId} disabled={true} value={selectedItemPath} className={styles.formInput} />
        </div>)}
              {!selectedFile?.isDirectory && selectedFile?.isSelected && selectedItemPath && selectedLakehouse?.id && selectedLakehouse?.workspaceId &&
                  <>
                      <Body1Stronger>File Preview</Body1Stronger>
                      {fileRows.length > 0 && (
                          <div className={styles.formInputGroup}>
                              <Button
                                  onClick={() => {
                                      onFileSelected(selectedFile, selectedLakehouse);
                                  }}>Use this file</Button>
                          </div>
                      )
                      }
                      <FilePreview
                          filePath={selectedItemPath}
                          lakehouseId={selectedLakehouse?.id}
                          workspaceId={selectedLakehouse?.workspaceId}
                          workloadClient={props.workloadClient}
                          onFileRowsLoaded={setFileRows}
                      />
                  </>
              }
      </div>
    </div>
  );
}