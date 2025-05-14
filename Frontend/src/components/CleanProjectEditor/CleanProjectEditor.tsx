import React, { useState, useEffect } from "react";
import { useLocation, useParams } from "react-router-dom";
import { Stack } from "@fluentui/react";
import {
  TabValue, Button,
  // Breadcrumb,
  // BreadcrumbItem,
  // BreadcrumbButton,
  // BreadcrumbDivider,
  Dialog,
  DialogSurface,
  DialogContent,
  DialogBody,
  DialogActions,
  DialogTitle,
  Tooltip} from "@fluentui/react-components";
// import {
//   FolderRegular,
//   EditRegular,
//   OpenRegular,
//   DocumentRegular,
//   PeopleRegular,
//   DocumentPdfRegular,
//   VideoRegular,
// } from "@fluentui/react-icons";

import { useTranslation } from "react-i18next";
import { initializeIcons } from "@fluentui/font-icons-mdl2";
import { AfterNavigateAwayData } from "@ms-fabric/workload-client";
import { ContextProps, PageProps } from "src/App";
import {
  callNavigationNavigate,
  callNavigationBeforeNavigateAway,
  callNavigationAfterNavigateAway,
  callThemeOnChange,
  callLanguageGet,
  callSettingsOnChange,
  callItemGet,
  callItemUpdate,
  callItemDelete,
  callRunItemJob,
  callAuthAcquireAccessToken,
} from "../../controller/CleanProjectController";
import { Ribbon } from "../CleanProjectRibbon/CleanProjectRibbon";
import { convertGetItemResultToWorkloadItem } from "../../utils";
import {
  CleanProjectItemClientMetadata,
  GenericItem,
  ItemPayload,
  UpdateItemPayload,
  WorkloadItem,
} from "../../models/CleanProjectModel";
import "./../../styles.scss";
import { ItemMetadataNotFound } from "../../models/WorkloadExceptionsModel";
import { FileMetadata } from "../../models/LakehouseExplorerModel";
import { CluedInConnection } from "src/models/CleanProjectModel";
import { Banner } from "./Banner";
import { CluedInConnectionInput } from "./CluedInConnection";
import { SourceFileSelector } from "./SourceFileSelector";
import { DestinationFileSelector } from "./DestinationFileSelector";
import { Tab, TabList } from '@fluentui/react-tabs';
import { CounterBadge } from "@fluentui/react-components";
import { makeStyles } from "@fluentui/react-components";
import { LoadingSkeleton } from "./LoadingSkeleton";
import { HomeScreen } from "./HomeScreen";
import { CopyRegular } from "@fluentui/react-icons";
import { callTestJob } from "../../controller/LakehouseExplorerController";

const useStyles = makeStyles({
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
    flexGrow: 2
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
});

interface WizardProps {
  props: PageProps;
  // isDialogOpen: boolean;
  // setIsDialogOpen: (value:boolean) => void;
  setDirty: (value: boolean) => void;
  cluedInConnection: CluedInConnection;
  setCluedInConnection: (value: CluedInConnection) => void;
  sourceFile: FileMetadata;
  setSourceFile: (value: FileMetadata) => void;
  destinationFile: FileMetadata;
  setDestinationFile: (value: FileMetadata) => void;
  setSelectedInputFileLakehouse: (value: GenericItem) => void;
  setSelectedOutputFileLakehouse: (value: GenericItem) => void;
  onFinish: () => void;
  onCancel: () => void;
}
const Wizard = ({
  props,
  setDirty,
  sourceFile,
  setSourceFile,
  destinationFile,
  setDestinationFile,
  cluedInConnection,
  setCluedInConnection,
  setSelectedInputFileLakehouse,
  setSelectedOutputFileLakehouse,
  onFinish,
  onCancel,
}: WizardProps) => {
  const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false);
  const [isNextEnabled, setIsNextEnabled] = useState<boolean>(false);
  const [wizardStep, setWizardStep] = useState<number>(1);
  const styles = useStyles();

  const getColor = (tabIndex: number) => {
    return tabIndex <= wizardStep ? "brand" : "informative";
  }
  const isLastStep = wizardStep === 3;
  const tabNames = ["connection", "input-file", "output-file"]
  return (
    <>
      {!isDialogOpen && <Banner {...props} onOpenWizard={() => {
        setIsDialogOpen(true);
      }} />}
      {isDialogOpen && <LoadingSkeleton {...props} />}
      <Dialog modalType="alert"
        open={isDialogOpen}
        onOpenChange={async (event, data) => {
          setIsDialogOpen(data.open);
        }}>
        <DialogSurface
          className={styles.dialogContainer}>
          <DialogTitle>
            Setup
            <TabList className={styles.stepsContainer} disabled={true} selectedValue={tabNames[wizardStep - 1]}>
              <Tab value="connection" data-testid="connection-tab-btn" className={styles.steps}><CounterBadge count={1} color={getColor(1)} /> <span>CluedIn Connection</span></Tab>
              <Tab value="input-file" data-testid="input-file-tab-btn" className={styles.steps}><CounterBadge count={2} color={getColor(2)} /> <span>Input File</span></Tab>
              <Tab value="output-file" data-testid="output-file-tab-btn" className={styles.steps}><CounterBadge count={3} color={getColor(3)} /> <span> Output</span></Tab>
            </TabList>
          </DialogTitle>
          <DialogBody className={styles.dialogBody}>
            <DialogContent className={styles.dialogContent}>
              {wizardStep === 1 &&
                <CluedInConnectionInput
                  {...props}
                  cluedInConnection={cluedInConnection}
                  onCancel={() => {
                    onCancel();
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
            </DialogContent>
            <DialogActions className={styles.dialogActions}>
              {wizardStep > 1 && <Button onClick={() => {
                setIsNextEnabled(false);
                setWizardStep(wizardStep - 1);
              }}>Back</Button>}
              {wizardStep < 4 && <Button appearance="primary" disabled={!isNextEnabled} onClick={() => {
                setIsNextEnabled(false);
                setWizardStep(wizardStep + 1);
                if (isLastStep) {
                  setIsDialogOpen(false);
                  onFinish();
                }
              }}>{ isLastStep ? "Finish" : "Next" }</Button>}
              <Button onClick={() => {
                setIsDialogOpen(false);
                onCancel();
              }}>Cancel</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </>);
}


export function CleanProjectEditor(props: PageProps) {
  const workloadBackendUrl = process.env.WORKLOAD_BE_URL;
  const workloadName = process.env.WORKLOAD_NAME;
  const { workloadClient } = props;
  const pageContext = useParams<ContextProps>();
  const { pathname } = useLocation();
  const { i18n } = useTranslation();

  // initializing usage of FluentUI icons
  initializeIcons();

  // React state for WorkloadClient APIs
  const [cleanProjectItem, setCleanProjectItem] =
    useState<WorkloadItem<ItemPayload>>(undefined);
  const [isDirty, setDirty] = useState<boolean>(false);
  const [isLoaded, setIsLoaded] = useState<boolean>(false);
  const [showLaunchDialog, setShowLaunchDialog] = useState<boolean>(false);
  const [isRunningSetupCleanProject, setIsRunningSetupCleanProject] = useState<boolean>(false);
  const [isRunningCleanInFabric, setIsRunningCleanInFabric] = useState<boolean>(false);
  const [isLaunchCleanEnabled, setIsLaunchCleanEnabled] = useState<boolean>(false);
  const [isCopied, setIsCopied] = useState<boolean>(false);

  const [selectedInputFileLakehouse, setSelectedInputFileLakehouse] =
    useState<GenericItem>(undefined);
  const [selectedOutputFileLakehouse, setSelectedOutputFileLakehouse] =
    useState<GenericItem>(undefined);
  const [sourceFile, setSourceFile] = useState<FileMetadata>(null);
  const [destinationFile, setDestinationFile] = useState<FileMetadata>(null);
  const [cluedInConnection, setCluedInConnection] = useState<CluedInConnection>(null);

  const params = useParams<ContextProps>();
  const itemObjectId = cleanProjectItem?.id || params.itemObjectId;
  const metadata = cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata;
  const isNew = !metadata?.domain
    && !metadata?.organizationName
    && !metadata?.userEmail
    && !metadata?.userPassword
    && !metadata?.inputFileLakehouse
    && !metadata?.outputFileLakehouse
    && !metadata?.inputFilePath
    && !metadata?.outputFilePath;

  const [, setLang] = useState<string>('en-US');
  const [, setItemEditorErrorMessage] = useState<string>("");
  document.body.dir = i18n.dir();

  const [selectedTab, setSelectedTab] = useState<TabValue>("home");

  useEffect(() => {
    callLanguageGet(workloadClient).then((lang) => setLang(lang));

    // Controller callbacks registrations:
    // register Blocking in Navigate.BeforeNavigateAway (for a forbidden url)
    callNavigationBeforeNavigateAway(workloadClient);

    // register a callback in Navigate.AfterNavigateAway
    callNavigationAfterNavigateAway(afterNavigateCallBack, workloadClient);

    // register Theme.onChange
    callThemeOnChange(workloadClient);

    // register Settings.onChange
    callSettingsOnChange(workloadClient, i18n.changeLanguage);
  }, []);


  useEffect(() => {
    loadDataFromUrl(pageContext, pathname);
  }, [pageContext, pathname]);


  async function afterNavigateCallBack(_event: AfterNavigateAwayData): Promise<void> {
    //clears the data after navigation
    setSelectedInputFileLakehouse(undefined);
    setSelectedOutputFileLakehouse(undefined);
    setSourceFile(undefined);
    setDestinationFile(undefined);
    setCluedInConnection(undefined);
    setCleanProjectItem(undefined);
    return;
  }

  async function loadDataFromUrl(
    pageContext: ContextProps,
    pathname: string
  ): Promise<void> {
    setIsLoaded(false);
    if (pageContext.itemObjectId) {
      // for Edit scenario we get the itemObjectId and then load the item via the workloadClient SDK
      const itemId = pageContext.itemObjectId;
      await loadItem(itemId);
      
    } else {
      console.log(`non-editor context. Current Path: ${pathname}`);
      clearItemData();
    }
    setIsLoaded(true);
  }

  async function loadItem(itemId: string) {
    try {
      const getItemResult = await callItemGet(
        itemId,
        workloadClient
      );
      const item =
        convertGetItemResultToWorkloadItem<ItemPayload>(getItemResult);

      setCleanProjectItem(item);

      // load extendedMetadata
      setValuesFromItem(item);

      setItemEditorErrorMessage("");
    } catch (error) {
      clearItemData();
      if (error?.ErrorCode === ItemMetadataNotFound) {
        setItemEditorErrorMessage(error?.Message);
        return;
      }

      console.error(
        `Error loading the Item (object ID:${itemId}`,
        error
      );
    }
  }

  function setValuesFromItem(item: WorkloadItem<ItemPayload>) {
    const metadata: CleanProjectItemClientMetadata = item?.extendedMetdata?.cleanProjectItemMetadata;
    setSelectedInputFileLakehouse(metadata?.inputFileLakehouse);
    setSelectedOutputFileLakehouse(metadata?.outputFileLakehouse);
    const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';
    if (metadata?.inputFilePath){
      const lastIndex = metadata?.inputFilePath?.lastIndexOf('/');
      setSourceFile({
        name: metadata?.inputFilePath?.substring(lastIndex + 1),
        path:  EMPTY_GUID + '/' + metadata?.inputFilePath?.substring(0, lastIndex),
        isDirectory: false,
        isSelected: true,
        contentLength: 0,
      });
    }
    if (metadata?.outputFilePath){
      const lastIndex = metadata?.outputFilePath?.lastIndexOf('/');
      setDestinationFile({
        name: metadata?.outputFilePath?.substring(lastIndex + 1),
        path:  EMPTY_GUID + '/' + metadata?.outputFilePath?.substring(0, lastIndex),
        isDirectory: true,
        isSelected: true,
        contentLength: 0,
      });
    }
    setCluedInConnection({
      domain: metadata?.domain,
      organizationName: metadata?.organizationName,
      userEmail: metadata?.userEmail,
      userPassword: metadata?.userPassword,
    });
    
    if (metadata?.cleanProjectId && metadata?.cleanProjectId !== EMPTY_GUID) {
      setIsLaunchCleanEnabled(true);
    }
  }

  function clearItemData() {
    setCleanProjectItem(undefined);
  }

  async function SaveItem() {
    // call ItemUpdate with the current payload contents
    console.log('Begin save item');
    const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';
    var inputFilePath = sourceFile ? `${sourceFile.path}/${sourceFile.name}`.substring(EMPTY_GUID.length + 1) : null;
    var outputFilePath = destinationFile ? `${destinationFile.path}/${destinationFile.name}`.substring(EMPTY_GUID.length + 1) : null;
    let payload: UpdateItemPayload = {
      cleanProjectItemMetadata
        : {
        inputFileLakehouse: selectedInputFileLakehouse,
        outputFileLakehouse: selectedOutputFileLakehouse,
        inputFilePath: inputFilePath,
        outputFilePath: outputFilePath,
        outputFileFormat: null,
        organizationName: cluedInConnection?.organizationName,
        domain: cluedInConnection?.domain,
        userEmail: cluedInConnection?.userEmail,
        userPassword: cluedInConnection?.userPassword,
        mappingJson: cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata?.mappingJson,
        notebookId: cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata?.notebookId,
        cleanProjectId: cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata?.cleanProjectId,
        currentStatus: cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata?.currentStatus,
        setupCleanProjectJobId: cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata?.setupCleanProjectJobId,
        cleanInFabricJobId: cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata?.cleanInFabricJobId,
      },
    };
    await callItemUpdate(itemObjectId, payload, workloadClient);

    setDirty(false);
  }

  async function deleteCurrentItem() {
    if (cleanProjectItem) {
      await deleteItem(cleanProjectItem.id);
      // navigate to workspaces page after delete
      await callNavigationNavigate("host", `/groups/${cleanProjectItem.workspaceId}`, workloadClient);
    }
  }

  async function deleteItem(itemId: string) {
    await callItemDelete(itemId, workloadClient);
  }

  // function getItemObjectId() {
  //   const params = useParams<ContextProps>();
  //   return sampleItem?.id || params.itemObjectId;
  // }

  async function checkItem(jobInstanceId: string) {
    let accessToken = await callAuthAcquireAccessToken(props.workloadClient, null);
    const testJobResult = await callTestJob(
      workloadBackendUrl,
      cleanProjectItem.workspaceId,
      itemObjectId,
      jobInstanceId,
      accessToken.token
    );
    if (testJobResult.status == "Failed" 
      || testJobResult.status == "Cancelled"
      || testJobResult.status == "Deduped"    
      || testJobResult.status == "Completed"      
    )
    {
      return { shouldContinue: false, status: testJobResult.status};
    }

    return { shouldContinue: true, status: testJobResult.status};;
  }

  function pollJob(jobInstanceId: string, onStopPolling: (status: string)=> void) {
    setTimeout(async () => {
      const checkItemResult = await checkItem(jobInstanceId);
      
      if (checkItemResult.shouldContinue) {
        setTimeout(async() => {
          await pollJob(jobInstanceId, onStopPolling);
        }, 10000);
      } else {
        const getItemResult = await callItemGet(
          itemObjectId,
          workloadClient
        );
        const item =
          convertGetItemResultToWorkloadItem<ItemPayload>(getItemResult);
        setCleanProjectItem(item);
        setValuesFromItem(item);
        onStopPolling(checkItemResult.status);
      }
    }, 10000);
  }

  async function onSetupCleanProject() {
    if (isDirty) {
      await SaveItem();
    }
    setIsRunningSetupCleanProject(true);
    const jobInstance = await callRunItemJob(
      itemObjectId,
      `${workloadName}.CleanProjectItem.SetupCleanProject`,
      JSON.stringify({ metadata: 'JobMetadata' }),
      true /* showNotification */,
      workloadClient);
    
    
      pollJob(jobInstance.itemJobInstanceId, (status) => {
        const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';
        if (metadata?.cleanProjectId && metadata?.cleanProjectId !== EMPTY_GUID) {
          setIsLaunchCleanEnabled(true);
        }
        setIsRunningSetupCleanProject(false);
      });
  }

  async function onCleanInFabric() {
    setIsRunningCleanInFabric(true);
    const jobInstance = await callRunItemJob(
      itemObjectId,
      `${workloadName}.CleanProjectItem.CleanInFabric`,
      JSON.stringify({ metadata: 'JobMetadata' }),
      true /* showNotification */,
      workloadClient);

    pollJob(jobInstance.itemJobInstanceId, (status) => {
        setIsRunningCleanInFabric(false);
      });
  }
  
  async function onLaunchInCluedIn() {
    setShowLaunchDialog(true);
  }
  
  const cleanUrl = `https://${cluedInConnection?.organizationName}.${cluedInConnection?.domain}/admin/preparation/new-clean-project/${cleanProjectItem?.extendedMetdata?.cleanProjectItemMetadata?.cleanProjectId}`;
  // HTML page contents
  return (
    <Stack className="editor" data-testid="clean-project-editor-inner">
      <Ribbon
        {...props}
        isLakeHouseSelected={selectedInputFileLakehouse != undefined && selectedOutputFileLakehouse != undefined}
        //  disable save when in Frontend-only
        isSaveButtonEnabled={
          cleanProjectItem?.id !== undefined &&
          selectedInputFileLakehouse != undefined &&
          selectedOutputFileLakehouse != undefined &&
          isDirty
        }
        saveItemCallback={SaveItem}
        isDeleteEnabled={cleanProjectItem?.id !== undefined}
        deleteItemCallback={deleteCurrentItem}
        itemObjectId={itemObjectId}
        onTabChange={setSelectedTab}
        isDirty={isDirty}
        setupCleanProjectCallback={onSetupCleanProject}
        launchInCluedInCallback={onLaunchInCluedIn}
        isLaunchCleanEnabled={isLaunchCleanEnabled}
      />

      <Stack className="main">
        {selectedTab == "home" && (
          <span>
            {isNew && isLoaded && <Wizard
              props={props}
              setDirty={setDirty}
              cluedInConnection={cluedInConnection}
              setCluedInConnection={setCluedInConnection}
              sourceFile={sourceFile}
              setSourceFile={setSourceFile}
              destinationFile={destinationFile}
              setDestinationFile={setDestinationFile}
              setSelectedInputFileLakehouse={setSelectedInputFileLakehouse}
              setSelectedOutputFileLakehouse={setSelectedOutputFileLakehouse}
              onFinish={async() => {
                if (isDirty) {
                  setIsLoaded(false);
                  await SaveItem();
                  await loadItem(itemObjectId);
                  setIsLoaded(true);
                }
              }}
              onCancel={() => {
                setValuesFromItem(cleanProjectItem);
              }}
            />}
            {!isLoaded && (
              <LoadingSkeleton {...props} />
            )}
            {!isNew && isLoaded && <HomeScreen 
              props={props}
              cleanProjectItem={cleanProjectItem}
              setDirty={setDirty}
              cluedInConnection={cluedInConnection}
              setCluedInConnection={setCluedInConnection}
              sourceFile={sourceFile}
              setSourceFile={setSourceFile}
              destinationFile={destinationFile}
              setDestinationFile={setDestinationFile}
              selectedInputFileLakehouse={selectedInputFileLakehouse}
              setSelectedInputFileLakehouse={setSelectedInputFileLakehouse}
              selectedOutputFileLakehouse={selectedOutputFileLakehouse}
              setSelectedOutputFileLakehouse={setSelectedOutputFileLakehouse}
              onSave={async() => {
                if (isDirty) {
                  setIsLoaded(false);
                  await SaveItem();
                  await loadItem(itemObjectId);
                  setIsLoaded(true);
                }
              }}
              onCancel={() => {
                setValuesFromItem(cleanProjectItem);
              }}
              onSetupCleanProject={async () => {
                await onSetupCleanProject();
              }}
              onCleanInFabric={async() => {
                await onCleanInFabric();
              }}
              isRunningSetupCleanProject={isRunningSetupCleanProject}
              isRunningCleanInFabric={isRunningCleanInFabric}
              isLaunchCleanEnabled={isLaunchCleanEnabled}
              onLaunchInCluedIn={async () => {
                await onLaunchInCluedIn();
              }}
            />}
            <Dialog 
              open={showLaunchDialog}
              onOpenChange={async (event, data) => {
                setShowLaunchDialog(data.open);
              }}>
              <DialogSurface>
                <DialogTitle>
                </DialogTitle>
                <DialogBody>
                  <DialogContent >
                    <div>Please copy the following link and open it in a new browser tab to access the CluedIn clean project.</div>
                    <div>{cleanUrl}<Tooltip content={isCopied ? "Copied": "Copy"} relationship="label" {...props}>
                      <Button icon={<CopyRegular />} size="small" onClick={() => {
                        navigator.clipboard.writeText(cleanUrl);
                        setIsCopied(true);
                        setInterval(() => setIsCopied(false), 3000);
                      }} />
                    </Tooltip></div>

                  </DialogContent>
                  <DialogActions >
                    <Button onClick={() => {
                      setShowLaunchDialog(false);
                    }}>Close</Button>
                  </DialogActions>
                </DialogBody>
              </DialogSurface>
            </Dialog>
          </span>
        )}
      </Stack>
    </Stack>
  );
}
