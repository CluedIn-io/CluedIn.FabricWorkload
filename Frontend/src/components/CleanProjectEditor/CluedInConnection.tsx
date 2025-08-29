import React, {
    //useEffect, 
    useState
} from "react";
import {
    Button,
    useId, Input, Label,
    Spinner,
    Caption1,
    PresenceBadge
} from "@fluentui/react-components";

import { CluedInConnectionProps } from "../../App";
import "./../../styles.scss";
import { makeStyles } from "@fluentui/react-components";
import { callAuthAcquireAccessToken } from "../../controller/SampleWorkloadController";
import { callTestConnection, TestConnectionResult } from "../../controller/LakehouseExplorerController";
import { CluedInConnection, CreateOrganizationRequest } from "../../models/CleanProjectModel";
import { Stack } from "@fluentui/react";
import { createOrganization, callNotificationOpen } from "../../controller/CleanProjectController";
import { NotificationToastDuration, NotificationType } from "@ms-fabric/workload-client";
import { CopyRegular } from "@fluentui/react-icons";

export const useStyles = makeStyles({
    formInputGroup: {
        // Stack the label above the field with a gap
        display: "grid",
        gridTemplateRows: "repeat(1fr)",
        justifyItems: "start",
        gap: "2px",
        maxWidth: "400px",
        marginBottom: '20px'
    },
    controlRow: {
        width: '400px',
    },
    formInput: {
        width: '400px',
    },
});


export const CluedInConnectionInput = (props: CluedInConnectionProps) => {
    const createUrl = (cluedInConnection: CluedInConnection) => {
        const organizationName = cluedInConnection?.organizationName;
        const domain = cluedInConnection?.domain;

        if (!organizationName || !domain) {
            return null;
        }

        return `https://${organizationName}.${domain}`;
    }
    const workloadBackendUrl = process.env.WORKLOAD_BE_URL;
    const { cluedInConnection, onCluedInConnectionUpdated, onCancel } = props;
    //const organizationNameId = useId("input-organizationName");
    const accountNameId = useId("input-accountName");
    const urlId = useId("input-url");
    const userEmailId = useId("input-userEmail");
    const userPasswordId = useId("input-userPassword");
    const styles = useStyles();
    const [organizationName, setOrganizationName] = useState<string>(cluedInConnection?.organizationName);
    const [url, setUrl] = useState<string>(createUrl(cluedInConnection));
    const [accountName, setAccountName] = useState<string>();
    const [domain, setDomain] = useState<string>(cluedInConnection?.domain);
    const [userEmail, setUserEmail] = useState<string>(cluedInConnection?.userEmail);
    const [userPassword, setUserPassword] = useState<string>(cluedInConnection?.userPassword);
    const [isTestingConnection, setIsTestingConnection] = useState<boolean>(false);
    const [isRegisterMode, setIsRegisterMode] = useState<boolean>(false);
    const [isCreatingAccount, setIsCreatingAccount] = useState<boolean>(false);
    const [isAccountCreated, setIsAccountCreated] = useState<boolean>(false);
    const [testConnectionResult, setTestConnectionResult] = useState<TestConnectionResult>(undefined);

    const setUrlInvalid = (message: string) => {
        setTestConnectionResult({
            isValid: false,
            message: message,
        })
        setDomain(null);
        setOrganizationName(null);
    }

    const setUrlPartsFromUrl = (url: string) => {
        if (!url) {
            setUrlInvalid("URL must not be empty");
            return;
        }
        if (!url.startsWith("https://")) {
            setUrlInvalid("URL must start with https://");
            return;
        }
        const urlWithoutTrailingSlash = url.endsWith("/") ? url.substring(0, url.length - 1) : url;
        const urlWithoutProtocol = urlWithoutTrailingSlash.substring("https://".length);
        if (urlWithoutProtocol.indexOf("/") != -1) {
            setUrlInvalid("Invalid URL. It must be in the form of https://myOrganizationName.myDomain");
            return;
        }
        const indexOfDot = urlWithoutProtocol.indexOf(".");

        if (indexOfDot == -1) {
            setUrlInvalid("Unable to find organization name. It must be in the form of https://myOrganizationName.myDomain");
            return;
        }

        const extractedOrg = urlWithoutProtocol.substring(0, indexOfDot);
        const extractedDomain = urlWithoutProtocol.substring(indexOfDot + 1);

        setDomain(extractedDomain);
        setOrganizationName(extractedOrg);
        setTestConnectionResult(undefined);
    }

    async function testConnection(additionalScopesToConsent: string): Promise<TestConnectionResult> {
        let accessToken = await callAuthAcquireAccessToken(props.workloadClient, additionalScopesToConsent);
        let result = await callTestConnection(workloadBackendUrl, {
            organizationName,
            domain,
            userEmail,
            userPassword,
        },
            accessToken.token);
        setTestConnectionResult(result);
        return result;
    }

    async function createAccount(additionalScopesToConsent: string): Promise<void> {
        let accessToken = await callAuthAcquireAccessToken(props.workloadClient, additionalScopesToConsent);
        const request: CreateOrganizationRequest = {
            organizationName: accountName,
            userEmail,
            userPassword,
        };

        try {
            var result = await createOrganization(
                workloadBackendUrl,
                request,
                accessToken.token,
            );
            setIsCreatingAccount(false);
            onCluedInConnectionUpdated({
                organizationName: result.organizationName,
                domain: result.domain,
                userEmail: result.userEmail,
                userPassword: result.userPassword,
            });
            await callNotificationOpen(
                "Account Creation", 
                `Successfully created a trial account. Your CluedIn URL is https://${result.organizationName}.${result.domain}`,
                 NotificationType.Success, 
                 NotificationToastDuration.Medium, 
                 props.workloadClient);
            setUrl(`https://${result.organizationName}.${result.domain}`);
        } catch {
            console.error('failed to create organization');
        }
    }

    return (
        <div>
            {!isRegisterMode && (<div className={styles.formInputGroup}>
                <Label size="medium" htmlFor={urlId} weight="semibold" style={{ paddingInlineEnd: "12px" }}>
                    URL
                </Label>
                <Input size="medium"
                    id={urlId}
                    className={styles.formInput}
                    value={url || ""}
                    onChange={(_, v) => {
                        setUrl(v.value);
                        setUrlPartsFromUrl(v.value);
                    }} />
                <div>
                    <div><Caption1>Organization Name: {organizationName}</Caption1></div>
                    <div><Caption1>Domain: {domain}</Caption1></div>
                </div>
            </div>)}
            {isRegisterMode && (<div className={styles.formInputGroup}>
                <Label size="medium" htmlFor={accountNameId} weight="semibold" style={{ paddingInlineEnd: "12px" }}>
                    Account Name
                </Label>
                <Input size="medium"
                    id={accountNameId}
                    className={styles.formInput}
                    value={accountName || ""}
                    onChange={(_, v) => {
                        setAccountName(v.value);
                    }} />
                <div>
                    {isAccountCreated && <div><Caption1>CluedIn URL: {url}<Button icon={<CopyRegular />} size="small" onClick={() => {
                        navigator.clipboard.writeText(url);
                      }} /></Caption1></div>}
                </div>
            </div>)}
            {/* <div className={styles.formInputGroup}>
                <Label size="medium" htmlFor={organizationNameId} weight="semibold" style={{ paddingInlineEnd: "12px" }}>
                    Organization Name
                </Label>
                <Input size="medium" id={organizationNameId} value={organizationName || ""} className={styles.formInput} onChange={(_, v) => setOrganizationName(v.value)} />
            </div> */}
            <div className={styles.formInputGroup}>
                <Label size="medium" htmlFor={userEmailId} weight="semibold" style={{ paddingInlineEnd: "12px" }}>
                    Username
                </Label>
                <Input size="medium" id={userEmailId} className={styles.formInput} value={userEmail || ""} onChange={(_, v) => setUserEmail(v.value)} />
            </div>
            <div className={styles.formInputGroup}>
                <Label size="medium" htmlFor={userPasswordId} weight="semibold" style={{ paddingInlineEnd: "12px" }}>
                    Password
                </Label>
                <Input size="medium" id={userPasswordId} type="password" className={styles.formInput} value={userPassword || ""} onChange={(_, v) => setUserPassword(v.value)} />
            </div>
            <div className={styles.controlRow}>
                <Stack horizontal horizontalAlign="space-between">
                    {!isRegisterMode && <Stack horizontal>
                        <Button
                            disabled={isTestingConnection}
                            onClick={async () => {
                                setIsTestingConnection(true);
                                const result = await testConnection(null);
                                setIsTestingConnection(false);
                                if (result?.isValid === true) {
                                    onCluedInConnectionUpdated({
                                        organizationName,
                                        domain,
                                        userEmail,
                                        userPassword,
                                    });
                                } else {
                                    onCancel();
                                }
                            }}>{!isTestingConnection && "Test"}
                            {isTestingConnection && <Spinner size="extra-small" />}</Button>
                        {testConnectionResult?.isValid === true && <span><PresenceBadge status="available" /> Valid</span>}
                        {testConnectionResult?.isValid === false && <span><PresenceBadge status="do-not-disturb" /> {testConnectionResult.message}</span>}
                    </Stack>}
                    
                    {props.canCreateNewAccount && !isRegisterMode && (
                        <Button onClick={() => {
                            setIsRegisterMode(true);
                        }}>Register New Account</Button>)
                    }

                    {props.canCreateNewAccount && isRegisterMode && (
                        <Button disabled={isCreatingAccount} onClick={() => {
                            setIsRegisterMode(false);
                        }}>Use Existing Account</Button>)
                    }

                    {isRegisterMode && <Button disabled={isCreatingAccount} onClick={async() => {
                        console.log("Account name: " + accountName);
                        setIsAccountCreated(false);
                        setIsCreatingAccount(true);
                        await createAccount(null);
                        setIsAccountCreated(true);
                    }}>{isCreatingAccount && <Spinner size="extra-small" />}{!isCreatingAccount && "Register" }</Button>}
                </Stack>
            </div>
        </div>
    )
}