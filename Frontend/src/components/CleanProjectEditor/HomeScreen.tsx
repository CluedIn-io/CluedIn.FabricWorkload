import React, { useState } from "react";
import {
  Image, Button, Card, CardHeader,
  Text, Caption1,
  mergeClasses, LargeTitle,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  CardFooter,
  Label,
  Spinner,
  DialogTitle,
  Tooltip,
  //Link, 
  //Title2
  } from "@fluentui/react-components";

import { PageProps } from "src/App";
import "./../../styles.scss";
import { makeStyles } from "@fluentui/react-components";
import { CluedInConnection, GenericItem, ItemPayload, WorkloadItem } from "src/models/CleanProjectModel";
import { FileMetadata } from "src/models/LakehouseExplorerModel";
import { CluedInConnectionInput } from "./CluedInConnection";
import { DestinationFileSelector } from "./DestinationFileSelector";
import { SourceFileSelector } from "./SourceFileSelector";
import { Open24Regular } from "@fluentui/react-icons";
import { PresenceBadge } from "@fluentui/react-components";
import { FilePreview } from "./FilePreview";
import { Stack } from "@fluentui/react";


const useStyles = makeStyles({
  bannerContainer: {
    display: 'flex',
    flexDirection: 'column',
  },
  banner: {
    display: 'flex',
    flexDirection: 'column',
    margin: '0 auto',
    textAlign: 'center',
  },
  bannerImage: {
    width: '50%',
    margin: '0 auto'
  },
  bannerText: {
    textAlign: 'center',
  },
  title: { margin: "0 0 12px" },
  cardCarousel: {
    display: 'flex',
    flexDirection: 'row',
    justifyContent: 'center',
    flexWrap: 'wrap',
  },
  card: {
    width: "300px",
    maxWidth: "100%",
    height: "175px",
    margin: '10px 10px',
    display: 'flex',
    flexDirection: 'column',
  },
  flex: {
    gap: "4px",
    display: "flex",
    flexDirection: "row",
    alignItems: "left",
  },
  dialogContainer: {
    // width: '80vw',
    // minHeight: '80vh',
    minWidth: '1000px',
    display: 'flex',
    flexDirection: 'column',
  },
  dialogHeader: {
    display: 'flex',
    flexDirection: 'row',
  },
  stepsContainer: {
    paddingBottom: '10px',
  },
  steps: {
    minWidth: '300px',
  },
  dialogBody: {
    display: 'flex',
    flexDirection: 'column',
    flexGrow: 2,
    maxHeight: '80vh',
    maxWidth: '80vw',
  },
  dialogContent: {
    flexGrow: 2
  },
  dialogActions: {
    display: 'flex',
    flexDirection: 'row',
    width: '100%',
    alignItems: 'right',
    justifyContent: 'flex-end'
  },
  appIcon: {
    borderRadius: "4px",
    height: "32px",
  },
  caption: {
    //color: tokens.colorNeutralForeground3,
  },
  captionDetails: {
    display: 'flex',
    flexDirection: 'row',
    flexWrap: 'wrap',
  },
  captionLabel: {
    paddingRight: '3px',
  },
  cardHeader: {
    flexGrow: 2,
    flexDirection: 'column',
  },
  cardFooter: {
    alignItems: "center",
    justifyContent: "space-between",
  },
  buttonCarousel: {
    display: 'flex',
    flexDirection: 'row',
    justifyContent: 'center'
  },
  buttonCarouselButton: {
    margin: '10px',
  },
});
interface HomeScreenProps {
  props: PageProps;
  cleanProjectItem: WorkloadItem<ItemPayload>;
  setDirty: (value: boolean) => void;
  cluedInConnection: CluedInConnection;
  setCluedInConnection: (value: CluedInConnection) => void;
  sourceFile: FileMetadata;
  setSourceFile: (value: FileMetadata) => void;
  destinationFile: FileMetadata;
  setDestinationFile: (value: FileMetadata) => void;
  selectedInputFileLakehouse: GenericItem;
  setSelectedInputFileLakehouse: (value: GenericItem) => void;
  selectedOutputFileLakehouse: GenericItem;
  setSelectedOutputFileLakehouse: (value: GenericItem) => void;
  onSave: () => void;
  onCancel: () => void;
  onSetupCleanProject: () => Promise<void>;
  onCleanInFabric: () => Promise<void>;
  isRunningSetupCleanProject: boolean;
  isRunningCleanInFabric: boolean;
  onLaunchInCluedIn: () => Promise<void>;
  isLaunchCleanEnabled: boolean;
}

export const HomeScreen = ({ 
  props, 
  cleanProjectItem,
  setDirty,
  sourceFile,
  setSourceFile,
  destinationFile,
  setDestinationFile,
  cluedInConnection,
  setCluedInConnection,
  selectedInputFileLakehouse,
  setSelectedInputFileLakehouse,
  selectedOutputFileLakehouse,
  setSelectedOutputFileLakehouse, 
  onSave,
  onCancel,
  onSetupCleanProject,
  onCleanInFabric,
  isRunningSetupCleanProject,
  isRunningCleanInFabric,
  onLaunchInCluedIn,
  isLaunchCleanEnabled,
}: HomeScreenProps) => {
  const styles = useStyles();
  const cluedInUrl = `https://${cluedInConnection?.organizationName}.${cluedInConnection?.domain}`;
  const [isModificationEnabled, setIsModificationEnabled] = useState<boolean>(true);
  const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false);
  const [wizardStep, setWizardStep] = useState<number>(0);
  const [isNextEnabled, setIsNextEnabled] = useState<boolean>(false);
  const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';
  const metadata = cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata;
  const validSetupCleanProject = metadata?.setupCleanProjectJobId && metadata?.setupCleanProjectJobId !== EMPTY_GUID;
  const validCleanInFabric = metadata?.cleanInFabricJobId && metadata?.cleanInFabricJobId !== EMPTY_GUID;
  const setupCleanAppearance = !validSetupCleanProject && !validCleanInFabric ? "primary" : 'outline';
  const cleanInFabricAppearance = validSetupCleanProject && !validCleanInFabric ? 'primary' : 'outline';;
  const previewAppearance = validCleanInFabric ? 'primary' : 'outline';
  var inputFilePath = sourceFile ? `${sourceFile.path}/${sourceFile.name}`.substring(EMPTY_GUID.length + 1) : null;
  var outputFilePath = destinationFile ? `${destinationFile.path}/${destinationFile.name}`.substring(EMPTY_GUID.length + 1) : null;
  //const cleanProjectId = cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata?.cleanProjectId;

  const isButtonsDisabled = !isModificationEnabled || isRunningSetupCleanProject || isRunningCleanInFabric;
  return (
    <div className={styles.bannerContainer}>
      <div className={styles.banner}>
        <Image src="../../../internalAssets/cleanse-created.png" className={styles.bannerImage} />
        <LargeTitle className={styles.bannerText}>Your CluedIn Cleanse has been configured</LargeTitle>
        {/* <Title2 className={styles.bannerText}>Let's configure Clean customer data</Title2> */}
      </div>
      {/* <div className={styles.buttonCarousel}>
        <Button appearance="primary" className={styles.buttonCarouselButton} onClick={() => {
          onOpenWizard();
        }}>Point to source</Button>
        <Button className={styles.buttonCarouselButton}>Help documentation</Button>
      </div> */}
      <div className={styles.cardCarousel}>
        <Card className={styles.card} {...props}>
          <CardHeader
            className={mergeClasses(styles.flex, styles.cardHeader)}
            header={
              <Text weight="semibold">
                CluedIn Instance Details {cluedInUrl && cluedInConnection?.userEmail && <PresenceBadge />}
              </Text>
            }
            description={
              <Caption1 className={styles.caption}>
                <Stack horizontal wrap><Label className={styles.captionLabel}>URL :</Label><Tooltip content={cluedInUrl} relationship="label"><Text truncate>{cluedInUrl}</Text></Tooltip></Stack>
                <Stack horizontal wrap><Label className={styles.captionLabel}>User:</Label><Text>{cluedInConnection?.userEmail}</Text></Stack>
              </Caption1>
            }
          />

          <CardFooter className={mergeClasses(styles.flex, styles.cardFooter)}>
            <Button disabled={isButtonsDisabled} onClick={() => {
              setWizardStep(1);
              setIsDialogOpen(true);
            }}>Modify</Button>
          </CardFooter>
        </Card>
        <Card className={styles.card} {...props}>
          <CardHeader
            className={mergeClasses(styles.flex, styles.cardHeader)}
            header={
              <Text weight="semibold">
                Source file {selectedInputFileLakehouse?.displayName && inputFilePath && <PresenceBadge />}
              </Text>
            }
            description={
              <Caption1 className={styles.caption}>
                <div><Label className={styles.captionLabel}>Lakehouse:</Label><Text>{selectedInputFileLakehouse?.displayName}</Text></div>
                <div><Label className={styles.captionLabel}>File Path:</Label><Text>{inputFilePath}</Text></div>
              </Caption1>
            }
          />
          <CardFooter className={mergeClasses(styles.flex, styles.cardFooter)}>
            <Button disabled={isButtonsDisabled} onClick={() => {
                setWizardStep(2);
                setIsDialogOpen(true);
              }}>Modify</Button>
          </CardFooter>
        </Card>
        <Card className={styles.card} {...props}>
          <CardHeader
            className={mergeClasses(styles.flex, styles.cardHeader)}
            header={
              <Text weight="semibold">
                Destination {selectedOutputFileLakehouse?.displayName && outputFilePath && <PresenceBadge />}
              </Text>
            }
            description={
              <Caption1 className={styles.caption}>
                <div><Label className={styles.captionLabel}>Lakehouse  :</Label><Text>{selectedOutputFileLakehouse?.displayName}</Text></div>
                <div><Label className={styles.captionLabel}>Folder Path:</Label><Text>{outputFilePath}</Text></div>
              </Caption1>
            }
          />

          <CardFooter className={mergeClasses(styles.flex, styles.cardFooter)}>
          <Button disabled={isButtonsDisabled} onClick={() => {
              setWizardStep(3);
              setIsDialogOpen(true);
            }}>Modify</Button>
          </CardFooter>
        </Card>
        <Card className={styles.card} {...props}>
          <CardHeader
            className={mergeClasses(styles.flex, styles.cardHeader)}
            header={
              <Text weight="semibold">
                Let CluedIn work its magic {validSetupCleanProject && validCleanInFabric && <PresenceBadge />}
              </Text> 
            }
            description={
              <>
                <Caption1 className={styles.caption}>First click Setup Clean Project. Then you can clean in fabric with the rules created from it</Caption1>
              </>
            }
          />

          <CardFooter className={mergeClasses(styles.flex, styles.cardFooter)}>
            <div>
              <Button
                appearance={setupCleanAppearance}
                disabled={isButtonsDisabled}
                onClick={async () => {
                  setIsModificationEnabled(false);
                  await onSetupCleanProject();
                  setIsModificationEnabled(true);
                }}>{!isRunningSetupCleanProject && "Setup Clean" }
                 {isRunningSetupCleanProject && <Spinner size="extra-small" />}</Button>
             
              {!isRunningSetupCleanProject && isLaunchCleanEnabled && (
                <Tooltip content="Launch in CluedIn" relationship="label">
                  <Button size="small" icon={<Open24Regular />} onClick={async () => {
                    await onLaunchInCluedIn();
                  }} />
              </Tooltip>)}
            </div>
            <div>
              <Button
                appearance={cleanInFabricAppearance}
                disabled={isButtonsDisabled || !validSetupCleanProject}
                onClick={async () => {
                  setIsModificationEnabled(false);
                  await onCleanInFabric();
                  setIsModificationEnabled(true);
                }}>{!isRunningCleanInFabric && "Clean In Fabric" }
                 {isRunningCleanInFabric && <Spinner size="extra-small" />}</Button>
            </div>
          </CardFooter>
        </Card>
        <Card className={styles.card} {...props}>
          <CardHeader
            className={mergeClasses(styles.flex, styles.cardHeader)}
            header={
              <Text weight="semibold">
                Review the cleaned data
              </Text>
            }
            description={
              <Caption1 className={styles.caption}>Data will be cleaned and transformed to conform to destination table needs</Caption1>
            }
          />

          <CardFooter className={mergeClasses(styles.flex, styles.cardFooter)}>
            <Button 
              disabled={isButtonsDisabled || !validCleanInFabric} 
              appearance={previewAppearance}
              onClick={() => {
                setWizardStep(4);
                setIsDialogOpen(true);
              }}>Preview</Button>
          </CardFooter>
        </Card>
      </div>
      <Dialog modalType="alert"
        open={isDialogOpen}
        onOpenChange={async (event, data) => {
          setIsDialogOpen(data.open);
        }}>
        <DialogSurface
          className={styles.dialogContainer}>
          <DialogTitle className={styles.stepsContainer}>
            <span>
              {wizardStep === 1 && "CluedIn Connection"}
              {wizardStep === 2 && "Input File"}
              {wizardStep === 3 && "Output"}
              {wizardStep === 4 && "Preview"}
            </span>
          </DialogTitle>
          <DialogBody className={styles.dialogBody}>
            <DialogContent className={styles.dialogContent}>
              {wizardStep === 1 &&
                <CluedInConnectionInput
                  {...props}
                  cluedInConnection={cluedInConnection}
                  onCancel={() => {
                    onCancel();
                    setIsNextEnabled(false);
                  }}
                  onCluedInConnectionUpdated={(connection: CluedInConnection) => {
                    setCluedInConnection(connection);
                    setDirty(true);
                    setIsNextEnabled(true);
                  }} />
              }
              {wizardStep === 2 &&
                <SourceFileSelector
                  {...props}
                  selectedFile={sourceFile}
                  onFileSelected={(file, lakehouse) => {
                    setSourceFile(file);
                    setSelectedInputFileLakehouse(lakehouse);
                    setDirty(true);
                    setIsNextEnabled(true);
                  }} />
              }
              {wizardStep === 3 &&
                <DestinationFileSelector
                  {...props}
                  selectedFile={destinationFile}
                  onFileSelected={(file, lakehouse) => {
                    setDestinationFile(file);
                    setSelectedOutputFileLakehouse(lakehouse);
                    setDirty(true);
                    setIsNextEnabled(true);
                  }} />
              }
              {wizardStep === 4 &&
              <>
                <div>
                  <Caption1 className={styles.caption}>
                    <Label className={styles.captionLabel} weight="semibold">Lakehouse  :</Label><Text>{selectedOutputFileLakehouse?.displayName} </Text>
                    <Label className={styles.captionLabel} weight="semibold">Folder Path:</Label><Text>{outputFilePath}</Text>
                  </Caption1>
                </div>
                <FilePreview
                  filePath={outputFilePath}
                  lakehouseId={selectedOutputFileLakehouse?.id}
                  workspaceId={selectedOutputFileLakehouse?.workspaceId}
                  workloadClient={props.workloadClient}
                   />
                </>
              }
            </DialogContent>
            <DialogActions className={styles.dialogActions}>
              {wizardStep < 4 && <Button appearance="primary" disabled={!isNextEnabled} onClick={() => {
                setIsNextEnabled(false);
                setWizardStep(0);
                setIsDialogOpen(false);
                onSave();
              }}>{ "Save" }</Button>}
              <Button onClick={() => {
                setWizardStep(0);
                setIsDialogOpen(false);
                onCancel();
              }}>Cancel</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
}