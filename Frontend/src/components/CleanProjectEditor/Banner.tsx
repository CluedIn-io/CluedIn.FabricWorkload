import React, {  } from "react";
import {
  Image, Button, Card, CardHeader,
  Text, Caption1,
  mergeClasses, LargeTitle, Title2
  } from "@fluentui/react-components";

import { BannerProps } from "src/App";
import "./../../styles.scss";
import { useStyles } from "./utils";

export const Banner = (props: BannerProps) => {
  const { onOpenWizard } = props;
  const styles = useStyles();
  return (
    <div className={styles.bannerContainer}>
      <div className={styles.banner}>
        <Image src="../../../internalAssets/cleanse-created.png" className={styles.bannerImage} />
        <LargeTitle className={styles.bannerText}>Your CluedIn Cleanse has been created</LargeTitle>
        <Title2 className={styles.bannerText}>Let's configure your Cleaning project to clean data</Title2>
      </div>
      <div className={styles.buttonCarousel}>
        <Button appearance="primary" className={styles.buttonCarouselButton} onClick={() => {
          onOpenWizard();
        }}>Point to source</Button>
        {/* <Button className={styles.buttonCarouselButton}>Help documentation</Button> */}
      </div>
      <div className={styles.cardCarousel}>
        <Card className={styles.card} {...props}>
          <header className={styles.flex}>

          </header>

          <CardHeader
            header={
              <Text weight="semibold">
                CluedIn Instance Details
              </Text>
            }
            description={
              <Caption1 className={styles.caption}>Key in details of your CluedIn Instance</Caption1>
            }
          />

          <footer className={mergeClasses(styles.flex, styles.cardFooter)}>
            <span></span>
            <span></span>
          </footer>
        </Card>
        <Card className={styles.card} {...props}>
          <header className={styles.flex}>

          </header>

          <CardHeader
            header={
              <Text weight="semibold">
                Point to source file
              </Text>
            }
            description={
              <Caption1 className={styles.caption}>Simply point to source file that you want to clean</Caption1>
            }
          />

          <footer className={mergeClasses(styles.flex, styles.cardFooter)}>
            <span></span>
            <span></span>
          </footer>
        </Card>
        <Card className={styles.card} {...props}>
          <header className={styles.flex}>

          </header>

          <CardHeader
            header={
              <Text weight="semibold">
                Choose your destination
              </Text>
            }
            description={
              <Caption1 className={styles.caption}>Select where your cleaned data should be saved within Microsoft Fabric</Caption1>
            }
          />

          <footer className={mergeClasses(styles.flex, styles.cardFooter)}>
            <span></span>
            <span></span>
          </footer>
        </Card>
        <Card className={styles.card} {...props}>
          <header className={styles.flex}>

          </header>

          <CardHeader
            header={
              <Text weight="semibold">
                Let CluedIn work its magic
              </Text>
            }
            description={
              <Caption1 className={styles.caption}>Your data is automatically cleaned, mapped and prepared for review, ready when you are</Caption1>
            }
          />

          <footer className={mergeClasses(styles.flex, styles.cardFooter)}>
            <span></span>
            <span></span>
          </footer>
        </Card>
        <Card className={styles.card} {...props}>
          <header className={styles.flex}>

          </header>

          <CardHeader
            header={
              <Text weight="semibold">
                Review the cleaned data
              </Text>
            }
            description={
              <Caption1 className={styles.caption}>Data will be cleaned and transformed to conform to destination table needs</Caption1>
            }
          />

          <footer className={mergeClasses(styles.flex, styles.cardFooter)}>
            <span></span>
            <span></span>
          </footer>
        </Card>
      </div>
    </div>
  );
}