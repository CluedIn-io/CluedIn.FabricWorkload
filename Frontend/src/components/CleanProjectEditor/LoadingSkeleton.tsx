import React, {  } from "react";
import {
  Image,
  Skeleton,
  SkeletonItem
} from "@fluentui/react-components";

import { PageProps } from "src/App";
import "./../../styles.scss";
import { makeStyles } from "@fluentui/react-components";

const useStyles = makeStyles({
    invertedWrapper: {
      //backgroundColor: tokens.colorNeutralBackground1,
    },
    firstRow: {
      alignItems: "center",
      display: "grid",
      paddingBottom: "10px",
      position: "relative",
      gap: "10px",
      gridTemplateColumns: "min-content 80%",
    },
    secondThirdRow: {
      alignItems: "center",
      display: "grid",
      paddingBottom: "10px",
      position: "relative",
      gap: "10px",
      gridTemplateColumns: "min-content 20% 20% 15% 15% 10%",
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
  });
  
export function LoadingSkeleton(props: PageProps) {
    const styles = useStyles();
    return (
    <div className={styles.invertedWrapper}>
      <div className={styles.banner}>
        <Image src="../../../internalAssets/cleanse-created.png" className={styles.bannerImage} />
      </div>
      <Skeleton {...props} aria-label="Loading Content">
        <div className={styles.firstRow}>
          <SkeletonItem shape="circle" size={24} />
          <SkeletonItem shape="rectangle" size={16} />
        </div>
        <div className={styles.secondThirdRow}>
          <SkeletonItem shape="circle" size={24} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
        </div>
        <div className={styles.secondThirdRow}>
          <SkeletonItem shape="square" size={24} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
        </div>
        <div className={styles.secondThirdRow}>
          <SkeletonItem shape="square" size={24} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
        </div>
        <div className={styles.secondThirdRow}>
          <SkeletonItem shape="square" size={24} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
        </div>
        <div className={styles.secondThirdRow}>
          <SkeletonItem shape="square" size={24} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
          <SkeletonItem size={16} />
        </div>
      </Skeleton>
    </div>);
  }