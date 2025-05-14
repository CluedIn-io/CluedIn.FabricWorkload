import React from "react";
import { Tab, TabList } from '@fluentui/react-tabs';
import { Toolbar } from '@fluentui/react-toolbar';

import {
  SelectTabEvent, SelectTabData, TabValue,
  //Menu, MenuItem, MenuList, MenuPopover, MenuTrigger,
  ToolbarButton, 
  //ToolbarDivider, Button, MenuButton, Label, Combobox, Option, 
  Tooltip,
  Text,
} from '@fluentui/react-components';
import {
  //Broom24Regular,
  // Chat24Regular,
  // Edit24Regular,
  // Share24Regular,
  // ZoomFit20Filled,
  Open24Regular,
  //ChatHelp24Regular,
} from "@fluentui/react-icons";
//import { Stack } from '@fluentui/react';

import { PageProps } from 'src/App';
import './../../styles.scss';
//import { ItemTabToolbar } from "./ItemTabToolbar";

const HomeTabToolbar = (props: RibbonProps) => {

  async function onLaunchInCluedInClicked() {
    await props.launchInCluedInCallback();
  }


  return (
    <Toolbar>
      <Tooltip
        content="Launch Cleaning Project In CluedIn"
        relationship="label">
        <ToolbarButton
          aria-label="Launch In CluedIn"
          data-testid="item-editor-launch-in-cluedin-btn"
          appearance="subtle"
          icon={<Open24Regular />} onClick={onLaunchInCluedInClicked}><Text>Launch In CluedIn</Text></ToolbarButton>
      </Tooltip>
    </Toolbar>
  );
};

export interface RibbonProps extends PageProps {
  saveItemCallback: () => Promise<void>;
  isLakeHouseSelected?: boolean;
  isSaveButtonEnabled?: boolean;
  isDeleteEnabled?: boolean;
  deleteItemCallback: () => void;
  itemObjectId?: string;
  onTabChange: (tabValue: TabValue) => void;
  isDirty: boolean;
  setupCleanProjectCallback: () => Promise<void>;
  launchInCluedInCallback: () => Promise<void>;
  isLaunchCleanEnabled: boolean;
  //cleanInFabricCallback: () => Promise<void>;
}

export function Ribbon(props: RibbonProps) {
  const { onTabChange } = props;
  const [selectedValue, setSelectedValue] = React.useState<TabValue>('home');

  const onTabSelect = (_: SelectTabEvent, data: SelectTabData) => {
    setSelectedValue(data.value);
    onTabChange(data.value);
  };

  return (
    <div className="ribbon">
      {/* <CollabButtons {...props} /> */}
      <TabList defaultSelectedValue="home" onTabSelect={onTabSelect} size="small">
        <Tab value="home" data-testid="home-tab-btn">Home</Tab>
        {/* <Tab value="connection" data-testid="connection-tab-btn">CluedIn Connection</Tab>
        <Tab value="input-file" data-testid="input-file-tab-btn">Input File</Tab>
        <Tab value="output-file" data-testid="output-file-tab-btn">Output</Tab> */}
      </TabList>

      <div className="toolbarContainer">
        {["home", "api"].includes(selectedValue as string) && <HomeTabToolbar {...props} />}
        {/* {selectedValue === "jobs" && <ItemTabToolbar {...props} />}
        {selectedValue === "fluentui" && <ViewTabToolbar {...props} />} */}
      </div>

    </div>
  );
};
