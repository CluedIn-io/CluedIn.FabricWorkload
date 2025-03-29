import React, { useState } from "react";
import { useParams } from "react-router-dom";
import { Stack } from "@fluentui/react";
import { Field, Input, Button, 
//    Checkbox
 } from "@fluentui/react-components";
import { PageProps, ContextProps } from "../../App";
import { callAuthAcquireAccessToken, callDialogClose, callItemCreate, callPageOpen } from "../../controller/CleanProjectController";
import { GenericItem, CreateItemPayload } from "../../models/CleanProjectModel";

interface SaveAsDialogProps extends PageProps {
    isImmediateSave?: boolean;
}

export function SaveAsDialog({ workloadClient, isImmediateSave }: SaveAsDialogProps) {
    const workloadName = process.env.WORKLOAD_NAME;
    // The type of the item in fabric is {workloadName}/{itemName}
    const itemType = workloadName + ".CleanProjectItem";
    const itemDisplayName = "CluedIn Cleanse";
    const itemEditorPath = "/clean-project-editor";
    const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

    const [displayName, setDisplayName] = useState<string>("");
    const [description, setDescription] = useState<string>("");
    const [isSaveDisabled, setIsSaveDisabled] = useState<boolean>(true);
    const [isSaveInProgress, setIsSaveInProgress] = useState<boolean>(false);
    const [validationMessage, setValidationMessage] = useState<string>(null);
    const [promptFullConsent, _] = useState<boolean>(false);

    const pageContext = useParams<ContextProps>();

    async function onSaveClicked() {
        let createResult: boolean;
        if (isImmediateSave) {
            try {
                setIsSaveInProgress(true);
                setIsSaveDisabled(true);
                
                // raise consent dialog for the user
                await callAuthAcquireAccessToken(workloadClient, null /*additionalScopesToConsent*/, null /*claimsForConditionalAccessPolicy*/, promptFullConsent);

                createResult = await handleCreateItem(pageContext.workspaceObjectId, displayName, description);
            } finally {
                setIsSaveInProgress(false);
                setIsSaveDisabled(false);
            }
        }

        if (createResult || !isImmediateSave) {
            callDialogClose(workloadClient);
        }
    }

    async function onCancelClicked() {
        callDialogClose(workloadClient);
    }

    function onDisplayNameChanged(newValue: string) {
        setDisplayName(newValue);
        setIsSaveDisabled(newValue.trim() == "" || isSaveInProgress);
    }

    function onDescriptionChanged(newValue: string) {
        setDescription(newValue);
    }

    const handleCreateItem = async (
        workspaceObjectId: string,
        displayName: string,
        description?: string): Promise<boolean> => {

        try {
            const createItemPayload: CreateItemPayload = {
                 cleanProjectItemMetadata: {
                     inputFileLakehouse: { id: EMPTY_GUID, workspaceId: EMPTY_GUID },
                     outputFileLakehouse: { id: EMPTY_GUID, workspaceId: EMPTY_GUID },
                     inputFilePath: null,
                     outputFilePath: null,
                     outputFileFormat: null,
                     organizationName: null,
                     domain: null,
                     userEmail: null,
                     userPassword: null,
                     mappingJson: null,
                     notebookId: EMPTY_GUID,
                     cleanProjectId: EMPTY_GUID,
                     setupCleanProjectJobId: EMPTY_GUID,
                     cleanInFabricJobId: EMPTY_GUID,
                     currentStatus: null,
                }
            };

            const createdItem: GenericItem = await callItemCreate(
                workspaceObjectId,
                itemType,
                displayName,
                description,
                createItemPayload,
                workloadClient);

            // open editor for the new item
            await callPageOpen(workloadName, `${itemEditorPath}/${createdItem.id}`, workloadClient);
        } catch (createError) {
            // name is already in use
            if (createError.error?.message?.code === "PowerBIMetadataArtifactDisplayNameInUseException") {
                setValidationMessage(`${itemDisplayName} name is already in use.`);
            }
            // capacity does not support this item type 
            else if (createError.error?.message?.code === "UnknownArtifactType") {
                setValidationMessage(`Workspace capacity does not allow ${itemDisplayName} creation`);
            }
            else {
                setValidationMessage(`There was an error while trying to create a new ${itemDisplayName}`);
            }
            console.error(createError);
            return false;
        }
        return true;
    };

    return (
        <Stack className="create-dialog" data-testid="create-item-dialog">
            <Stack className="section">
                <h2>Create {itemDisplayName}</h2>
                <Field label="Name:" validationMessage={validationMessage}>
                    <Input data-testid="create-dialog-name-input" onChange={e => onDisplayNameChanged(e.target.value)} defaultValue={displayName} disabled={isSaveInProgress} />
                </Field>
                <Field label="Description:">
                    <Input onChange={e => onDescriptionChanged(e.target.value)} defaultValue={description} disabled={isSaveInProgress} />
                </Field>
                {/* <Checkbox label ="Request Initial Consent (Mark this if this is the first time you're working with this workload)" onChange={(v) => setPromptFullConsent(v.target.checked)}/> */}
                <Stack className="create-buttons" horizontal tokens={{ childrenGap: 10 }}>
                    <Button appearance="primary" onClick={() => onSaveClicked()} data-testid="create-3p-item-button" disabled={isSaveDisabled}>Create</Button>
                    <Button appearance="secondary" onClick={() => onCancelClicked()}>Cancel</Button>
                </Stack>
            </Stack>
        </Stack>
    );
}
