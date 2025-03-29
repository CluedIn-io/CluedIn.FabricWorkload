import React, { useState } from "react";
import {
  useId, Input, Label, Dropdown, Option,
  Body1Stronger} from "@fluentui/react-components";
import { FileSelectionProps } from "src/App";
import {
  GenericItem,
} from "../../models/CleanProjectModel";
import "./../../styles.scss";
import { LakehouseFileExplorerComponent } from "../SampleWorkloadFileExplorer/SampleWorkloadFileExplorer";
import { FileMetadata } from "src/models/LakehouseExplorerModel";
import { removePrefix, useStyles } from "./utils";

export const DestinationFileSelector = (props: FileSelectionProps) => {
  const { workloadClient, onFileSelected } = props;
  const styles = useStyles();
  const selectedFolderInputId = useId("input-folder");
  const fileNameInputId = useId("input-fileName");
  const outputFormatInputId = useId("input-outputFormat");
  const [selectedFile, setSelectedFile] = useState<FileMetadata>(null);
  const [selectedLakehouse, setSelectedLakehouse] = useState<GenericItem>(null);
  const [_, setSelectedFileName] = useState<string>(null);
  const selectedItemPath = selectedFile
    ? removePrefix(selectedFile.path + '/' + selectedFile.name)
    : null;

  const options = ['JSON', 'CSV', 'Parquet'];
  return (
    <div className={styles.fileSelector}>
      <LakehouseFileExplorerComponent
        workloadClient={workloadClient}
        title={"Destination File"}
        selectFileInstructions={"Select the destination filie"}
        selectLakeHouseInstructions={"Select the lakehouse where the destination file should be located"}
        isFolderOnly={true}
        onFileSelected={(file, lakehouse) => {
          setSelectedFile(file);
          setSelectedLakehouse(lakehouse);
        }} />
      <div className={styles.fileSelectorInfoPanel}>
        {!selectedItemPath && (<div className={styles.formInputGroup}>
          <Body1Stronger>Please select the destination folder for the output file.</Body1Stronger>
        </div>)}
        {selectedItemPath && (<>
          <div className={styles.formInputGroup}>
            <Label size="medium" htmlFor={selectedFolderInputId} weight="semibold">
              Output Folder
            </Label>
            <Input size="medium" id={selectedFolderInputId} disabled={true} value={selectedItemPath} className={styles.formInput} />
          </div>
          <div className={styles.formInputGroup}>
            <Label size="medium" htmlFor={fileNameInputId} weight="semibold">
              File Name
            </Label>
            <Input size="medium" id={fileNameInputId} className={styles.formInput} onChange={(e, v) => {
              setSelectedFileName(v.value);
              onFileSelected({
                name: v.value,
                path: selectedFile.path + '/' + selectedFile.name,
                isDirectory: false,
                isSelected: true,
                contentLength: 0,
              }, selectedLakehouse);
            }} />
          </div>
          <div className={styles.formInputGroup}>
            <Label size="medium" htmlFor={outputFormatInputId} weight="semibold">
              Output Format
            </Label>
            <Dropdown id={outputFormatInputId} placeholder="Output Format" className={styles.formInput} value={'CSV'} disabled={true}>
              {options.map((option) => (
                <Option key={option}>
                  {option}
                </Option>
              ))}
            </Dropdown>
          </div>
        </>)}
      </div>
    </div>
  );
}