import React, { useEffect, useState } from "react";
import {

   // Body1Stronger,
    Spinner,
    TableCellLayout
} from "@fluentui/react-components";

import {
    useScrollbarWidth,
    useFluent,
    TableBody,
    TableCell,
    TableRow,
    Table,
    TableHeader,
    TableHeaderCell,
    createTableColumn,
    useTableFeatures,
    //Text
  } from '@fluentui/react-components';

import "./../../styles.scss";
//import { corruptItems, columns } from "./Data";
import { FileRow, getFilePreviewInLakehouse, getFilePreviewInLakehousePath } from "../../controller/LakehouseExplorerController";
import { callAuthAcquireAccessToken } from "../../controller/CleanProjectController";
import { WorkloadClientAPI } from "@ms-fabric/workload-client";
import { makeStyles } from "@fluentui/react-components";
import { FixedSizeList, ListChildComponentProps } from 'react-window';

export const useStyles = makeStyles({
    tableCell: {
        borderLeft: '1px solid silver',
        borderRadius: 'unset',
        textOverflow: 'ellipsis',
    },
    tableRow: {
        // borderLeft: '1px solid silver',
        // borderRight: '1px solid silver',
        borderBottom: '1px solid silver',
        // borderRadius: 'unset'
    },
    table: {
        border: '1px solid silver',
        borderRadius: 'unset',
    }
});


//   type Item = {
//     file: string;
//     author: string;
//     lastUpdated: string;
//     lastUpdate: string;
//   };
  
//   interface TableRowData extends RowStateBase<Item> {
//   }
  
//   interface ReactWindowRenderFnProps extends ListChildComponentProps {
//     data: TableRowData[];
//   }
//   const baseItems: Item[] = [
//     {
//       file: 'file',
//       author: 'author',
//       lastUpdated: 'lastupdated',
//       lastUpdate: 'last update',
//     }
//   ];
  
//   const items = new Array(1500)
//     .fill(0)
//     .map((_, i) => baseItems[i % baseItems.length]);

    
// const columns = [
//     createTableColumn<Item>({
//       columnId: 'file',
//     }),
//     createTableColumn<Item>({
//       columnId: 'author',
//     }),
//     createTableColumn<Item>({
//       columnId: 'lastUpdated',
//     }),
//     createTableColumn<Item>({
//       columnId: 'lastUpdate',
//     }),
//   ];

export function FilePreview({ 
    filePath,
    workspaceId,
    lakehouseId,
    workloadClient,
    onFileRowsLoaded,
}: { 
    filePath: string, 
    workspaceId: string, 
    lakehouseId: string, 
    workloadClient: WorkloadClientAPI,
    onFileRowsLoaded?: (rows: FileRow[]) => void;
}) {
    const workloadBackendUrl = process.env.WORKLOAD_BE_URL;
    const [loadingPreviewStatus, setLoadingPreviewStatus] = useState<string>("idle");
    const [filePreviewRows, setFilePreviewRows] = useState<FileRow[]>(null);
    const styles = useStyles();
  const { targetDocument } = useFluent();
  const scrollbarWidth = useScrollbarWidth({ targetDocument });

  const data = (filePreviewRows || []);

  const columnNames = (data && data.length > 0)
  ? Object.keys(data[0])
  : [];
  let columns = columnNames.map(columnName => createTableColumn<FileRow>({
    columnId: columnName,
  }))
  columns.unshift( createTableColumn<FileRow>({
    columnId: 'No',
  }));
  
  const RenderRow = ({ index, style, data }: ListChildComponentProps) => {
    const { item } = data[index];
    return (
      <TableRow className={styles.tableRow}
        aria-rowindex={index + 2}
        style={style}
        key={`previewrow_${index}`}
      >
        <TableCell className={styles.tableCell}>{index+1}</TableCell>
        {columnNames.map((column) => 
            <TableCell className={styles.tableCell}>
                <TableCellLayout truncate={true}>{item[column]}</TableCellLayout>
            </TableCell>
        )}
      </TableRow>
    );
  };
  const { getRows } = useTableFeatures({ items: data, columns });

  const rows = getRows((row) => {
    return row;
  });

    useEffect(() => {
        const fetchFilePreview = async () => {
            setLoadingPreviewStatus("loading");
            let success = false;
            try {
                success = await setFilePreview(null);
            } catch (exception) {
                success = false;
            }
            setLoadingPreviewStatus(success ? "idle" : "error");
        }
        fetchFilePreview();
    }, [filePath, lakehouseId, workspaceId]);

    async function setFilePreview(additionalScopesToConsent: string): Promise<boolean> {
        let accessToken = await callAuthAcquireAccessToken(workloadClient, additionalScopesToConsent);
        const filePreviewPath = getFilePreviewInLakehousePath(
            workloadBackendUrl,
            workspaceId,
            lakehouseId,
            filePath,
        );
        let fileRows = await getFilePreviewInLakehouse(filePreviewPath, accessToken.token);
        if (fileRows) {
            setFilePreviewRows(fileRows);
            if (onFileRowsLoaded) {
                onFileRowsLoaded(fileRows || []);
            }
            return true;
        }
        return false;
    }


    return (
        <div>
            {loadingPreviewStatus === "loading" && <Spinner />}
            {loadingPreviewStatus === "error" && <div>Error loading preview</div>}
            {
                loadingPreviewStatus === "idle" && (
                    <>
                        <Table 
                            noNativeElements
                            arial-label="Default table" 
                            size="extra-small" 
                            className={styles.table} >
                            <TableHeader >
                                <TableRow className={styles.tableRow} appearance={"neutral"}>
                                    <TableHeaderCell key={"rowIndex"} className={styles.tableCell}>
                                    </TableHeaderCell>
                                    {columnNames.map((column) => (
                                        <TableHeaderCell key={column} className={styles.tableCell}>
                                            <TableCellLayout truncate={true}>{column}</TableCellLayout>
                                        </TableHeaderCell>
                                    ))}
                                    <div role="presentation" style={{ width: scrollbarWidth }} />
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                <FixedSizeList
                                    height={400}
                                    itemCount={data.length}
                                    itemSize={25}
                                    width="100%"
                                    itemData={rows}
                                >
                                    {RenderRow}
                                </FixedSizeList>
                            </TableBody>
                        </Table>

                    </>)
            }
        </div>);
}