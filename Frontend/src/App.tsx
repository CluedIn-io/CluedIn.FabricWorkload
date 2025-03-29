import React from "react";
import { Route, Router, Switch } from "react-router-dom";
import { History } from "history";

import { WorkloadClientAPI } from "@ms-fabric/workload-client";

import { SampleWorkloadEditor, SamplePage } from "./components/SampleWorkloadEditor/SampleWorkloadEditor";
import { Authentication } from './components/SampleWorkloadAuthEditor/SampleWorkloadAuthEditor';
import { Panel } from "./components/SampleWorkloadPanel/SampleWorkloadPanel";
import { SaveAsDialog } from "./components/SampleWorkloadCreateDialog/SampleWorkloadCreateDialog";
import { SaveAsDialog as CleanProjectSaveAsDialog } from "./components/CleanProjectCreateDialog/CleanProjectCreateDialog";
import CustomItemSettings from "./components/CustomItemSettings/CustomItemSettings";
import CustomAbout from "./components/CustomItemSettings/CustomAbout";
import { CleanProjectEditor } from "./components/CleanProjectEditor/CleanProjectEditor";
import { FileMetadata } from "./models/LakehouseExplorerModel";
import { GenericItem, CluedInConnection } from "./models/CleanProjectModel";

/*
    Add your Item Editor in the Route section of the App function below
*/

interface AppProps {
    history: History;
    workloadClient: WorkloadClientAPI;
}

export interface CleanProjectPageProps {
    workloadClient: WorkloadClientAPI;
    history?: History;
    title: string;
    selectFileInstructions: string;
    selectLakeHouseInstructions: string;
    isFolderOnly: boolean;
    onFileSelected: (file: FileMetadata, lakehouse: GenericItem) => void;
}

export interface PageProps {
    workloadClient: WorkloadClientAPI;
    history?: History
}

export interface BannerProps {
    workloadClient: WorkloadClientAPI;
    history?: History;
    onOpenWizard: () => void;
}
export interface FileSelectionProps {
    workloadClient: WorkloadClientAPI;
    history?: History;
    selectedFile: FileMetadata;
    onFileSelected: (file: FileMetadata, lakehouse: GenericItem) => void;
}

export interface CluedInConnectionProps {
    workloadClient: WorkloadClientAPI;
    history?: History;
    cluedInConnection: CluedInConnection;
    onCluedInConnectionUpdated: (connection: CluedInConnection) => void;
    onCancel: () => void;
}

export interface ContextProps {
    itemObjectId?: string;
    workspaceObjectId?: string
}

export function App({ history, workloadClient }: AppProps) {
    return <Router history={history}>
        <Switch>
            {/* This is the routing to the Sample Workload Editor.
                 Add your workload editor path here, and reference it in index.worker.ts  */}
            <Route path="/sample-workload-editor/:itemObjectId">
                <SampleWorkloadEditor
                    workloadClient={workloadClient} data-testid="sample-workload-editor" />
            </Route>

            {/* This is the routing to the Sample Workload Frontend-ONLY experience.
                 Add your workload creator path here, and reference it in index.worker.ts  */}
            <Route path="/sample-workload-frontend-only">
                <SampleWorkloadEditor
                    workloadClient={workloadClient} data-testid="sample-workload-frontend-only" />
            </Route>

            {/* This is the routing to the Sample Workload Create Dialog experience, 
                where an Item will be saved and the Editor will be opened
                Add your workload creator path here, and reference it in index.worker.ts  */}
            <Route path="/sample-workload-create-dialog/:workspaceObjectId">
                <SaveAsDialog
                    workloadClient={workloadClient}
                    isImmediateSave={true} data-testid="sample-workload-create-dialog" />
            </Route>

            {/* Routing to a sample Panel  */}
            <Route path="/panel">
                <Panel
                    workloadClient={workloadClient} data-testid="sample-workload-panel" />
            </Route>

            {/* Routing to a sample Page  */}
            <Route path="/sample-page/:itemObjectId">
                <SamplePage workloadClient={workloadClient} history={history} data-testid="sample-page" />
            </Route>

            {/* Routing to an Authentication Editor */}
            <Route path="/Authentication/:itemObjectId">
                <Authentication workloadClient={workloadClient} history={history} data-testid="authentication-editor" />
            </Route>

            {/* Routing to Custom Item Settings */}
            <Route path="/custom-item-settings">
                <CustomItemSettings data-testid="custom-about" />
            </Route>
            <Route path="/custom-about">
                <CustomAbout />
            </Route>
            
            <Route path="/clean-project-editor/:itemObjectId">
                <CleanProjectEditor
                    workloadClient={workloadClient} data-testid="clean-project-editor" />
            </Route>
            
            <Route path="/clean-project-create-dialog/:workspaceObjectId">
                <CleanProjectSaveAsDialog
                    workloadClient={workloadClient}
                    isImmediateSave={true} data-testid="clean-project-create-dialog" />
            </Route>
        </Switch>
    </Router>;
}