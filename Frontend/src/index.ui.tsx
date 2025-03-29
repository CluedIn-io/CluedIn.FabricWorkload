import { createBrowserHistory } from "history";
import React from "react";
import { createRoot } from 'react-dom/client';

import { FluentProvider } from "@fluentui/react-components";
import { createWorkloadClient, InitParams } from '@ms-fabric/workload-client';

import { fabricLightTheme } from "./theme";
import { App } from "./App";
import { convertGetItemResultToWorkloadItem } from "./utils";
import { callItemGet } from "./controller/SampleWorkloadController";
import { ItemPayload, ItemTabActionContext } from "./models/SampleWorkloadModel";

export async function initialize(params: InitParams) {
    const workloadClient = createWorkloadClient();

    const history = createBrowserHistory();
    workloadClient.navigation.onNavigate((route) => history.replace(route.targetUrl));
    workloadClient.action.onAction(async function ({ action, data }) {
        switch (action) {
            case 'cleanProject.tab.onInit':
            case 'sample.tab.onInit':
                const { id } = data as ItemTabActionContext;
                try{
                    const getItemResult = await callItemGet(
                        id,
                        workloadClient
                    );
                    const item = convertGetItemResultToWorkloadItem<ItemPayload>(getItemResult);
                    return {title: item.displayName};
                } catch (error) {
                    console.error(
                        `Error loading the Item (object ID:${id})`,
                        error
                    );
                    return {};
                }
            case 'cleanProject.tab.canDeactivate':
            case 'sample.tab.canDeactivate':
                return { canDeactivate: true };
            case 'cleanProject.tab.onDeactivate':
            case 'sample.tab.onDeactivate':
                return {};
            case 'cleanProject.tab.canDestroy':
            case 'sample.tab.canDestroy':
                return { canDestroy: true };
            case 'cleanProject.tab.onDestroy':
            case 'sample.tab.onDestroy':
                return {};
            case 'cleanProject.tab.onDelete':
            case 'sample.tab.onDelete':
                return {};
            default:
                throw new Error('Unknown action received');
        }
    });
    const root = createRoot(document.getElementById('root'));
    root.render(
        <FluentProvider theme={fabricLightTheme}>
            <App history={history} workloadClient={workloadClient} />
        </FluentProvider>
    );
}
