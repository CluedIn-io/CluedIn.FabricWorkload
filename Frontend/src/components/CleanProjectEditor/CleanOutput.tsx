import React, { useState, useEffect } from "react";
import {
  Button, 
  Text, 
  
  Body1Stronger, 
  Subtitle2
} from "@fluentui/react-components";
import { Spinner } from "@fluentui/react-components";

import {
  TableBody,
  TableCell,
  TableRow,
  Table,
  TableHeader,
  TableHeaderCell,
} from "@fluentui/react-components";

import { PageProps } from "src/App";
import "./../../styles.scss";
import { columns, cleanItems } from "./Data";
import { useStyles } from "./utils";

export const CleanOutput = (props: PageProps) => {
  const [isPreview, setIsPreview] = useState<boolean>(false);
  const styles = useStyles();
  const [showSpinner, setShowSpinner] = useState<boolean>(true);
  useEffect(() => {
    setTimeout(() => {
      setShowSpinner(false);
    }, 2000);
  }, []);

  return (
    <div>
      {showSpinner && <>
        <Body1Stronger>Cleanse is running...</Body1Stronger>
        <Spinner />

      </>}
      {!isPreview && !showSpinner && (
        <div className={styles.outputSummary}>
          <Subtitle2>Cleanse summary</Subtitle2>
          <Table size="small">
            <TableHeader>
              <TableRow>
                <TableHeaderCell key="SourceFile"><Body1Stronger>Source File</Body1Stronger></TableHeaderCell>
                <TableHeaderCell key="DestinationFile"><Body1Stronger>Destination File</Body1Stronger></TableHeaderCell>
                <TableHeaderCell key="TotalRecords"><Body1Stronger>Total Records</Body1Stronger></TableHeaderCell>
                <TableHeaderCell key="StartTime"><Body1Stronger>Start time</Body1Stronger></TableHeaderCell>
                <TableHeaderCell key="Action"><Body1Stronger>Action</Body1Stronger></TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              <TableRow>
                <TableCell>/Files/Demo/corrupt_company_data.csv</TableCell>
                <TableCell>/Files/Demo/clean_company_data.csv</TableCell>
                <TableCell>10</TableCell>
                <TableCell>A minute ago</TableCell>
                <TableCell><Button className={styles.actionButton} onClick={() => {
                  setIsPreview(true);
                }}>Preview</Button></TableCell>
              </TableRow>
            </TableBody>
          </Table></div>)}
      {isPreview &&
        (<div className={styles.outputPreview}><Subtitle2>Output Preview (/Files/Demo/clean_company_data.csv)</Subtitle2>
          
          <Table arial-label="Default table" size="small">
            <TableHeader>
              <TableRow>
                {columns.map((column) => (
                  <TableHeaderCell key={column.columnKey}>
                    <Body1Stronger>{column.label}</Body1Stronger>
                  </TableHeaderCell>
                ))}
              </TableRow>
            </TableHeader>
            <TableBody>
              {cleanItems.map((item) => (
                <TableRow key={item.companyName}>
                  <TableCell>{item.companyName}</TableCell>
                  <TableCell>{item.industry}</TableCell>
                  <TableCell>{item.revenue}</TableCell>
                  <TableCell>{item.totalEmployees}</TableCell>
                  <TableCell>{item.headquarters}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <Text>Showing <Text weight="semibold">10</Text> out of <Text weight="semibold">10</Text> rows</Text>
        </div>)}
    </div>
  )
}