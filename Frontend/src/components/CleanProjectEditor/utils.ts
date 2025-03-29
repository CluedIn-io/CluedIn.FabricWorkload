
import { makeStyles } from "@fluentui/react-components";

export const removePrefix = (path: string) => {
    const index = path.indexOf("Files/");
    if (index != -1) {
      return path.substring(index);
    }
  
    return path;
  }
  
export const useStyles = makeStyles({
    main: {
      display: "flex",
      flexDirection: "column",
      flexWrap: "wrap",
      columnGap: "16px",
      rowGap: "36px",
    },
    bannerContainer: {
      display: 'flex',
      flexDirection: 'column',
    },
    banner: {
      display: 'flex',
      flexDirection: 'column',
      margin: '0 auto',
      textAlign: 'center',
    },
    bannerImage: {
      width: '50%',
      margin: '0 auto'
    },
    bannerText: {
      textAlign: 'center',
    },
    title: { margin: "0 0 12px" },
    cardCarousel: {
      display: 'flex',
      flexDirection: 'row',
      justifyContent: 'center',
      flexWrap: 'wrap',
    },
    card: {
      width: "300px",
      maxWidth: "100%",
      height: "150px",
      margin: '10px 10px',
    },
    flex: {
      gap: "4px",
      display: "flex",
      flexDirection: "row",
      alignItems: "center",
    },
    appIcon: {
      borderRadius: "4px",
      height: "32px",
    },
    caption: {
      //color: tokens.colorNeutralForeground3,
    },
    cardFooter: {
      alignItems: "center",
      justifyContent: "space-between",
    },
    buttonCarousel: {
      display: 'flex',
      flexDirection: 'row',
      justifyContent: 'center'
    },
    buttonCarouselButton: {
      margin: '10px',
    },
    fileSelector: {
      display: 'flex',
      flexDirection: 'row',
      minWidth: '400px',
    },
    fileSelectorInfoPanel: {
      width: '50vw',
      maxWidth: '50vw',
      padding: '5px 10px',
      margin: '0 auto',
    },
    outputSummary: {
      padding: '15px',
      minHeight: '50vh',
    },
    outputPreview: {
      padding: '15px',
      minHeight: '50vh',
    },
    actionButton: {
      margin: '3px'
    },
    formInputGroup: {
      // Stack the label above the field with a gap
      display: "grid",
      gridTemplateRows: "repeat(1fr)",
      justifyItems: "start",
      gap: "2px",
      maxWidth: "400px",
      marginBottom: '20px'
    },
    formInput: {
      width: '400px',
    },
  });