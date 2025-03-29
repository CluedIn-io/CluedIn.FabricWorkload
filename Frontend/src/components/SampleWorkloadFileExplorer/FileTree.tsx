import React from "react";
import { FileMetadata, LakehouseExplorerFilesTreeProps } from "src/models/LakehouseExplorerModel";
import { Folder20Regular, Document20Regular } from "@fluentui/react-icons";
import { TreeItem, TreeItemLayout, Tooltip, Tree } from "@fluentui/react-components";

const CreateTree = (level: number, file: FileMetadata, props: LakehouseExplorerFilesTreeProps, filesGroupByPath: { [key: string]: FileMetadata[]; }) => {
    const { onSelectFileCallback, isFolderOnly } = props;

    const subRootKey = file.path + '/' + file.name;

    const subTree = filesGroupByPath[subRootKey]
        ? (<Tree>
            {filesGroupByPath[subRootKey].map((child) => CreateTree(level + 1, child, props, filesGroupByPath))}
        </Tree>)
        : null;


    if (file.isDirectory) {
        return (
            <TreeItem
                key={file.name}
                accessKey={file.path}
                itemType="branch"
                onClick={() => onSelectFileCallback(file)}
            >
                <Tooltip
                    relationship="label"
                    content={file.name}>
                    <TreeItemLayout
                        className={`lvl${level} ` + (file.isSelected ? "selected" : "")}
                        iconBefore={<Folder20Regular />}>
                        {file.name}
                    </TreeItemLayout>
                </Tooltip>
                {subTree}
            </TreeItem>
        );
    }
    if (isFolderOnly) {
        return null;
    }

    return (
        <TreeItem
            key={file.name}
            accessKey={file.path}
            itemType="leaf"
            onClick={() => onSelectFileCallback(file)}
        >
            <Tooltip
                relationship="label"
                content={file.name}>

                <TreeItemLayout
                    className={`file-lvl${level} ` + (file.isSelected ? "selected" : "")}
                    iconBefore={<Document20Regular />}>
                    {file.name}
                </TreeItemLayout>
            </Tooltip>
        </TreeItem>
    );
}
export function FileTree(props: LakehouseExplorerFilesTreeProps) {
    const { allFilesInLakehouse } = props;
    const filesGroupByPath: { [key: string]: FileMetadata[] } = {};
    for (let file of (allFilesInLakehouse || [])) {
        if (!filesGroupByPath[file.path]) {
            filesGroupByPath[file.path] = [];
        }
        filesGroupByPath[file.path].push(file);
    }

    const keys = Object.keys(filesGroupByPath);
    const sortedKeys = keys.sort();
    if (sortedKeys.length == 0) {
        return null;
    }

    const rootKey = sortedKeys[0];
    const root = filesGroupByPath[rootKey];

    return (
        <>
            {root &&
                root.map((file) => CreateTree(1, file, props, filesGroupByPath))}
        </>
    );
}
