import { CluedInConnection } from "src/models/CleanProjectModel";
import { FileMetadata, TableMetadata } from "../models/LakehouseExplorerModel";

export const getTablesInLakehousePath = (baseUrl: string, workspaceObjectId: string, itemObjectId: string) => {
    return `${baseUrl}/onelake/${workspaceObjectId}/${itemObjectId}/tables`;
}

export async function getTablesInLakehouse(
    tablesPath: string,
    token: string): Promise<TableMetadata[]> {
    try {
        const response: Response = await fetch(tablesPath, { method: `GET`, headers: { 'Authorization': 'Bearer ' + token } });
        const responseBody: string = await response.text();
        const data = JSON.parse(responseBody);
        if (!data.ErrorCode) {
            return data;
        } else {
            return null;
        }
    }
    catch (error) {
        console.error(`Error fetching tables: ${error}`);
        return null;
    }
}
export const getFilesInLakehousePath = (baseUrl: string, workspaceObjectId: string, itemObjectId: string) => {
    return `${baseUrl}/onelake/${workspaceObjectId}/${itemObjectId}/files`;
}

export async function getFilesInLakehouse(
    tablesPath: string,
    token: string): Promise<FileMetadata[]> {
    try {
        const response: Response = await fetch(tablesPath, { method: `GET`, headers: { 'Authorization': 'Bearer ' + token } });
        const responseBody: string = await response.text();
        const data = JSON.parse(responseBody);
        if (!data.ErrorCode) {
            return data;
        } else {
            return null;
        }
    }
    catch (error) {
        console.error(`Error fetching tables: ${error}`);
        return null;
    }
}

export interface FileRow {
    [key: string] :string;
}
export const getFilePreviewInLakehousePath = (baseUrl: string, workspaceObjectId: string, itemObjectId: string, filePath: string) => {
    return `${baseUrl}/onelake/${workspaceObjectId}/${itemObjectId}/filePreview?path=${filePath}`;
}

export async function getFilePreviewInLakehouse(
    filePreviewPath: string,
    token: string): Promise<FileRow[]> {
    try {
        const response: Response = await fetch(filePreviewPath, { method: `GET`, headers: { 'Authorization': 'Bearer ' + token } });
        const responseBody: string = await response.text();
        const data = JSON.parse(responseBody);
        if (!data.ErrorCode) {
            return data;
        } else {
            return null;
        }
    }
    catch (error) {
        console.error(`Error fetching tables: ${error}`);
        return null;
    }
}
export interface TestConnectionResult {
    isValid: boolean;
    message: string;
}
export async function callTestConnection(
    url: string,
    connection: CluedInConnection,
    token: string): Promise<TestConnectionResult> {
    try {
        const response: Response = await fetch(url + '/connections/test', 
            { 
                method: `POST`, 
                headers: { 
                    'Authorization': 'Bearer ' + token,
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(connection)
            });
        const responseBody: string = await response.text();
        const data = JSON.parse(responseBody);
        return data;
    }
    catch (error) {
        console.error(`Error fetching tables: ${error}`);
        return null;
    }
}

export interface TestJobResult {
    id: string;
    startTimeUtc: string;
    status: string;
    jobType: string;
}

export async function callTestJob(
    baseUrl: string,
    workspaceId: string,
    itemId: string,
    itemJobInstanceId: string,
    token: string): Promise<TestJobResult> {
    try {
        const url = `${baseUrl}/itemJob/${workspaceId}/${itemId}/${itemJobInstanceId}`
        const response: Response = await fetch(url, 
            { 
                method: `GET`, 
                headers: { 
                    'Authorization': 'Bearer ' + token,
                },
            });
        const responseBody: string = await response.text();
        const data = JSON.parse(responseBody);
        return data;
    }
    catch (error) {
        console.error(`Error fetching tables: ${error}`);
        return null;
    }
}