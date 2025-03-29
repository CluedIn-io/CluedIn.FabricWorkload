import React from "react";
import { Tab, TabList } from '@fluentui/react-tabs';
import { Toolbar } from '@fluentui/react-toolbar';

import {
  SelectTabEvent, SelectTabData, TabValue,
  //Menu, MenuItem, MenuList, MenuPopover, MenuTrigger,
  ToolbarButton, 
  //ToolbarDivider, Button, MenuButton, Label, Combobox, Option, 
  Tooltip
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
          disabled={!props.isLaunchCleanEnabled}
          aria-label="Launch In CluedIn"
          data-testid="item-editor-launch-in-cluedin-btn"  
          icon={<Open24Regular />} onClick={onLaunchInCluedInClicked}>Launch In CluedIn</ToolbarButton>
      </Tooltip>

      {/* <Tooltip
        content="Clean In Fabric"
        relationship="label">
        <ToolbarButton
          aria-label="Clean In Fabric"
          data-testid="item-editor-save-btn"
          icon={<Broom24Regular />} onClick={onCleanInFabric}>Clean In Fabric</ToolbarButton>
      </Tooltip> */}

      {/* <Tooltip
        content="Help Documentation"
        relationship="label">
        <ToolbarButton
          aria-label="Help Documentation"
          data-testid="item-editor-help-documentation-btn"
          icon={<ChatHelp24Regular />}
          onClick={() => onHelpDocumentationClicked()}>Help Documentation</ToolbarButton>
      </Tooltip> */}
    </Toolbar>
  );
};

// const ViewTabToolbar = (props: RibbonProps) => {
//   const zoomOptions = [
//     "75%",
//     "90%",
//     "100%",
//     "120%",
//     "150%",
//   ];

//   return (
//     <Toolbar>
//       <ToolbarButton
//         aria-label="Zoom to fit"
//         icon={<ZoomFit20Filled />}>Zoom to fit</ToolbarButton>
//       <ToolbarDivider />

//       <Label className="comboboxLabel">Zoom:</Label>
//       <Combobox readOnly={true}
//         style={{ minWidth: "unset" }} input={{ style: { width: "50px" } }}
//         defaultValue="100%"
//         defaultSelectedOptions={["100%"]}>
//         {zoomOptions.map((option) => (
//           <Option key={option}>{option}</Option>
//         ))}
//       </Combobox>
//     </Toolbar>
//   );
// };

// const CollabButtons = (props: RibbonProps) => {
//   return (
//     <div className="collabContainer">
//       <Stack horizontal>
//         <Button size="small" icon={<Chat24Regular />}>Comments</Button>
//         <Menu>
//           <MenuTrigger disableButtonEnhancement>
//             <MenuButton size="small" icon={<Edit24Regular />}>Editing</MenuButton>
//           </MenuTrigger>
//           <MenuPopover>
//             <MenuList>
//               <MenuItem>Editing</MenuItem>
//               <MenuItem>Viewing</MenuItem>
//             </MenuList>
//           </MenuPopover>
//         </Menu>
//         <Button size="small" icon={<Share24Regular />} appearance="primary">Share</Button>
//       </Stack>
//     </div>
//   );
// }

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
      <TabList defaultSelectedValue="home" onTabSelect={onTabSelect}>
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
