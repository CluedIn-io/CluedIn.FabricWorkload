// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Fabric_Extension_BE_Boilerplate.Services.Ingestion;

namespace Boilerplate.Controllers;

internal partial class IngestionEndpointOperation
{
    private static class RequestTemplates
    {
        public static string GetCleanProjectDetailAsync(Guid cleanProjectId)
        {
            #region Request
            var requestString = $$"""
            query cleanProjectDetail($id: ID!) {
              preparation {
                cleanProjectDetail(id: $id) {
                  name
                  id
                  status
                  query
                  autoGenerateRules
                  __typename
                }
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "cleanProjectDetail",
              "variables": {
                "id": "{{cleanProjectId}}"
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string GetDataSetByIdAsync(Guid dataSetId)
        {
            #region Request
            var requestString = $$"""
            query getDataSetById($id: ID!) {
              inbound {
                id
                dataSet(id: $id) {
                  id
                  originalFields
                  fieldMappings {
                    originalField
                    key
                    id
                    __typename
                  }
                  __typename
                }
                __typename
              }
            }

            """;
            #endregion

            return $$"""
            {
              "operationName": "getDataSetById",
              "variables": {
                "id": "{{dataSetId}}"
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string CreateCleanProjectAsync(Guid providerDefinitionId, string name, Dictionary<string, string> mapping)
        {
            #region Request
            var requestString = $$"""
            mutation createNewCleanProject($cleanProject: InputNewCleanProject!) {
              preparation {
                id
                createNewCleanProject(cleanProject: $cleanProject) {
                  id
                  __typename
                }
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "createNewCleanProject",
              "variables": {
                "cleanProject": {
                  "name": "{{name}}",
                  "query": "",
                  "includeDataParts": false,
                  "fields": [{{string.Join(", ", mapping.Keys.Select(key =>
                  {
                        return $$"""
                               {
                                  "property": "{{key}}",
                                  "type": "vocabulary"
                               }
                               """;
                   }))}}
                  ],
                  "description": "[{\"type\":\"paragraph\",\"children\":[{\"text\":\"\"}]}]",
                  "condition": {
                    "rules": [
                      {
                        "condition": "AND",
                        "field": "ProviderDefinitionIds",
                        "objectTypeId": "0a1984a6-7b75-47a9-b076-8f6b45b358ac",
                        "operator": "4988d076-3ec1-4414-9f56-5b9b30e25f72",
                        "value": [
                          "{{providerDefinitionId}}"
                        ],
                        "type": "enumerable"
                      }
                    ],
                    "condition": "AND"
                  }
                }
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }

        public static string GenerateCleanResultAsync(Guid cleanProjectId)
        {
            #region Request
            var requestString = $$"""
            mutation generateResult($id: ID!) {
              preparation {
                generateResult(id: $id)
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "generateResult",
              "variables": {
                "id": "{{cleanProjectId}}"
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }

        public static string CommitDataSetAsync(Guid dataSetId)
        {
            #region Request
            var requestString = $$"""
            mutation commitDataSet($dataSetId: ID, $purgeQuarantine: Boolean) {
              inbound {
                commitDataSet(dataSetId: $dataSetId, purgeQuarantine: $purgeQuarantine)
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "commitDataSet",
              "variables": {
                "dataSetId": "{{dataSetId}}",
                "purgeQuarantine": true
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string GetAllVocabulariesAsync(
            string vocabularyName)
        {
            #region Request
            var requestString = $$"""
            query getAllVocabularies($searchName: String, $isActive: Boolean, $pageNumber: Int, $pageSize: Int, $sortBy: String, $sortDirection: String, $entityType: String, $connectorId: ID, $filterTypes: Int, $filterHasNoSource: Boolean) {
              management {
                id
                vocabularies(
                  searchName: $searchName
                  isActive: $isActive
                  pageNumber: $pageNumber
                  pageSize: $pageSize
                  sortBy: $sortBy
                  sortDirection: $sortDirection
                  entityType: $entityType
                  connectorId: $connectorId
                  filterTypes: $filterTypes
                  filterHasNoSource: $filterHasNoSource
                ) {
                  total
                  data {
                    vocabularyId
                    vocabularyName
                    keyPrefix
                    isCluedInCore
                    isDynamic
                    isProvider
                    isActive
                    grouping
                    createdAt
                    connector {
                      id
                      name
                      about
                      icon
                      __typename
                    }
                    __typename
                  }
                  __typename
                }
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "getAllVocabularies",
              "variables": {
                "searchName": "{{vocabularyName}}",
                "pageNumber": 1,
                "pageSize": 100,
                "entityType": null,
                "connectorId": null,
                "isActive": null,
                "filterTypes": null,
                "filterHasNoSource": null
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string CreateVocabularyAsync(
            string vocabularyName,
            string entityType)
        {
            #region Request
            var requestString = $$"""
            mutation createVocabulary($vocabulary: InputVocabulary) {
              management {
                id
                createVocabulary(vocabulary: $vocabulary) {
                  ...Vocabulary
                  __typename
                }
                __typename
              }
            }

            fragment Vocabulary on Vocabulary {
              vocabularyId
              vocabularyName
              keyPrefix
              isCluedInCore
              entityTypeConfiguration {
                icon
                entityType
                displayName
                __typename
              }
              isDynamic
              isProvider
              isActive
              grouping
              createdAt
              providerId
              description
              connector {
                id
                name
                about
                icon
                __typename
              }
              __typename
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "createVocabulary",
              "variables": {
                "vocabulary": {
                  "vocabularyName": "{{vocabularyName}}",
                  "entityTypeConfiguration": {
                    "icon": "Twitter",
                    "new": false,
                    "displayName": "{{entityType}}",
                    "entityType": "/{{entityType}}"
                  },
                  "providerId": "",
                  "keyPrefix": "{{vocabularyName}}",
                  "description": ""
                }
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string CreateAutoAnnotationAsync(
            Guid dataSetId,
            string entityType,
            string vocabularyName,
            Guid vocabularyId)
        {
            #region Request
            var requestString = $$"""
            mutation createAutoAnnotation($dataSetId: ID!, $type: String!, $mappingConfiguration: InputMappingConfiguration, $isDynamicVocab: Boolean) {
              management {
                createAutoAnnotation(
                  dataSetId: $dataSetId
                  type: $type
                  mappingConfiguration: $mappingConfiguration
                  isDynamicVocab: $isDynamicVocab
                ) {
                  id
                  __typename
                }
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "createAutoAnnotation",
              "variables": {
                "dataSetId": "{{dataSetId}}",
                "type": "file",
                "mappingConfiguration": {
                  "entityTypeConfiguration": {
                    "icon": "Twitter",
                    "new": false,
                    "displayName": "{{entityType}}",
                    "entityType": "/{{entityType}}"
                  },
                  "ignoredFields": [],
                  "vocabularyConfiguration": {
                    "new": false,
                    "keyPrefix": "{{vocabularyName}}",
                    "vocabularyName": "{{vocabularyName}}",
                    "vocabularyId": "{{vocabularyId}}"
                  }
                },
                "isDynamicVocab": true
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string GetEntityTypeInfoAsync(
            string entityType,
            bool withPageTemplate)
        {
            #region Request
            var requestStringWithoutPageTemplate = $$"""
            query getEntityTypeInfo($type: String!) {
              management {
                getEntityTypeInfo(type: $type) {
                  id
                  icon
                  displayName
                  type
                  route
                  path
                  active
                  layoutConfiguration
                  pageTemplateId
                  __typename
                }
                __typename
              }
            }
            """;
            var requestStringWithPageTemplate = $$"""
            query getEntityTypeInfo($type: String!) {
              management {
                getEntityTypeInfo(type: $type, includePageTemplate: false) {
                  id
                  icon
                  displayName
                  type
                  route
                  path
                  active
                  layoutConfiguration
                  pageTemplateId
                  __typename
                }
                __typename
              }
            }
            """;
            var requestString = withPageTemplate ? requestStringWithPageTemplate : requestStringWithoutPageTemplate;
            #endregion

            return $$"""
            {
              "operationName": "getEntityTypeInfo",
              "variables": {
                "type": "/{{entityType}}"
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string CreateEntityTypeAsync(
            string entityType,
            string entityTypeRoute)
        {
            #region Request

            var variable = $$"""
            {
              "type": "/{{entityType}}",
              "active": true,
              "displayName": "{{entityType}}",
              "icon": "Twitter",
              "route": "{{entityTypeRoute}}",
              "pageTemplateId": ""
            }
            """;
            var requestString = $$"""
            mutation createEntityTypeConfigurationV2($entityTypeConfiguration: String!) {
              management {
                createEntityTypeConfigurationV2(
                  entityTypeConfiguration: $entityTypeConfiguration
                )
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "createEntityTypeConfigurationV2",
              "variables": {
                "entityTypeConfiguration": {{GraphqlQueryHelper.Serialize(variable)}}
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string GetAnnotationById(
            int annotationId)
        {
            #region Request
            var requestString = $$"""
            query getAnnotationById($id: ID) {
              preparation {
                id
                annotation(id: $id) {
                  id
                  annotationCodeSetup
                  isDynamicVocab
                  name
                  entityType
                  previewImageKey
                  nameKey
                  descriptionKey
                  originEntityCodeKey
                  createdDateMap
                  modifiedDateMap
                  cultureKey
                  origin
                  versionKey
                  beforeCreatingClue
                  beforeSendingClue
                  useStrictEdgeCode
                  useDefaultSourceCode
                  vocabularyId
                  vocabulary {
                    vocabularyName
                    vocabularyId
                    providerId
                    keyPrefix
                    __typename
                  }
                  entityTypeConfiguration {
                    icon
                    displayName
                    entityType
                    __typename
                  }
                  annotationProperties {
                    key
                    vocabKey
                    coreVocab
                    useAsEntityCode
                    useAsAlias
                    useSourceCode
                    entityCodeOrigin
                    vocabularyKeyId
                    type
                    annotationEdges {
                      id
                      key
                      edgeType
                      entityTypeConfiguration {
                        icon
                        displayName
                        entityType
                        __typename
                      }
                      origin
                      dataSourceGroupId
                      dataSourceId
                      dataSetId
                      direction
                      edgeProperties {
                        id
                        annotationEdgeId
                        originalField
                        vocabularyKey {
                          displayName
                          vocabularyKeyId
                          isCluedInCore
                          isDynamic
                          isObsolete
                          isProvider
                          vocabularyId
                          name
                          isVisible
                          key
                          mappedKey
                          groupName
                          dataClassificationCode
                          dataType
                          description
                          providerId
                          mapsToOtherKeyId
                          __typename
                        }
                        __typename
                      }
                      __typename
                    }
                    vocabularyKey {
                      displayName
                      vocabularyKeyId
                      isCluedInCore
                      isDynamic
                      isObsolete
                      isProvider
                      vocabularyId
                      name
                      isVisible
                      key
                      mappedKey
                      groupName
                      dataClassificationCode
                      dataType
                      description
                      providerId
                      mapsToOtherKeyId
                      __typename
                    }
                    validations {
                      id
                      displayName
                      inverse
                      parameters {
                        key
                        value
                        __typename
                      }
                      __typename
                    }
                    transformations {
                      filters {
                        parameters {
                          key
                          value
                          __typename
                        }
                        id
                        displayName
                        inverse
                        __typename
                      }
                      operations {
                        inverse
                        parameters {
                          key
                          value
                          __typename
                        }
                        id
                        displayName
                        __typename
                      }
                      __typename
                    }
                    __typename
                  }
                  __typename
                }
                __typename
              }
            }
            """;
            #endregion
            return $$"""
            {
              "operationName": "getAnnotationById",
              "variables": {
                "id": "{{annotationId}}"
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string SetOriginEntityCodeKeyAsync(
            int annotationId,
            string originEntityCodeKey)
        {
            #region Request
            var requestString = $$"""
            mutation modifyAnnotation($annotation: InputEntityAnnotation) {
              preparation {
                modifyAnnotation(annotation: $annotation)
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "modifyAnnotation",
              "variables": {
                "annotation": {
                  "id": "{{annotationId}}",
                  "originEntityCodeKey": "{{originEntityCodeKey}}"
                }
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string GetDataSourceByIdAsync(
            int dataSourceId)
        {
            #region Request
            var requestString = $$"""
            query getDataSourceById($id: ID!) {
              inbound {
                dataSource(id: $id) {
                  id
                  canBeDeleted
                  name
                  hasError
                  latestErrorMessage
                  errorType
                  author {
                    id
                    username
                    __typename
                  }
                  fileMetadata {
                    fileName
                    processing
                    uploading
                    uploadedPercentage
                    mimeType
                    __typename
                  }
                  createdAt
                  type
                  dataSourceSet {
                    id
                    name
                    __typename
                  }
                  sql
                  connectionStatus {
                    connected
                    errorMessage
                    __typename
                  }
                  dataSets {
                    id
                    name
                    annotationId
                    elasticTotal
                    expectedTotal
                    annotation {
                      originEntityCodeKey
                      annotationProperties {
                        key
                        __typename
                      }
                      __typename
                    }
                    stats {
                      total
                      successful
                      failed
                      __typename
                    }
                    author {
                      id
                      username
                      __typename
                    }
                    createdAt
                    updatedAt
                    dataSource {
                      id
                      __typename
                    }
                    __typename
                  }
                  connectorConfigurationId
                  connectorConfiguration {
                    id
                    name
                    accountDisplay
                    accountId
                    active
                    autoSync
                    codeName
                    configuration
                    connector {
                      id
                      icon
                      name
                      authMethods
                      properties
                      streamModes
                      __typename
                    }
                    createdDate
                    entityId
                    failingAuthentication
                    guide
                    helperConfiguration
                    providerId
                    reAuthEndpoint
                    source
                    sourceQuality
                    stats
                    status
                    supportsAutomaticWebhookCreation
                    supportsConfiguration
                    supportsWebhooks
                    userId
                    userName
                    users {
                      id
                      username
                      roles
                      __typename
                    }
                    webhookManagementEndpoints
                    webhooks
                    __typename
                  }
                  __typename
                }
                __typename
              }
            }

            """;
            #endregion

            return $$"""
            {
              "operationName": "getDataSourceById",
              "variables": {
                "id": "{{dataSourceId}}"
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }

        public static string CreateDataSourceSetAsync(
            string dataSourceSetName,
            Guid userId)
        {
            #region Request
            var requestString = $$"""
            mutation createDataSourceSet($dataSourceSet: InputDataSourceSet) {
              inbound {
                createDataSourceSet(dataSourceSet: $dataSourceSet)
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "createDataSourceSet",
              "variables": {
                "dataSourceSet": {
                  "name": "{{dataSourceSetName}}",
                  "author": "{{userId}}"
                }
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
        public static string CreateDataSetAsync(
            int dataSourceId,
            Guid userId,
            string entityType)
        {
            #region Request
            var requestString = $$"""
            mutation createDataSets($dataSourceId: ID, $dataSets: [InputDataSet]) {
              inbound {
                createDataSets(dataSourceId: $dataSourceId, dataSets: $dataSets) {
                  id
                  __typename
                }
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "createDataSets",
              "variables": {
                "dataSourceId": "{{dataSourceId}}",
                "dataSets": [
                  {
                    "author": "{{userId}}",
                    "store": true,
                    "name": "MyIngestEndpointName",
                    "type": "endpoint",
                    "configuration": {
                      "object": {
                        "endPointName": "MyIngestEndpointName",
                        "autoSubmit": false,
                        "entityType": "/{{entityType}}"
                      },
                      "entityTypeConfiguration": {
                        "icon": "MdHourglassTop",
                        "new": true,
                        "displayName": "{{entityType}}",
                        "entityType": "/{{entityType}}"
                      }
                    }
                  }
                ]
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }

        public static string CreateDataSourceAsync(
            int dataSourceSetId,
            string dataSourceName,
            Guid userId)
        {
            #region Request
            var requestString = $$"""
            mutation createDataSource($dataSourceSetId: ID, $dataSource: InputDataSource) {
              inbound {
                createDataSource(dataSourceSetId: $dataSourceSetId, dataSource: $dataSource) {
                  id
                  __typename
                }
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "createDataSource",
              "variables": {
                "dataSourceSetId": "{{dataSourceSetId}}",
                "dataSource": {
                  "author": "{{userId}}",
                  "type": "endpoint",
                  "name": "{{dataSourceName}}"
                }
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }

        public static string ModifyDataSetAutoSubmitAsync(Guid dataSetId)
        {
            #region Request
            var requestString = $$"""
            mutation modifyDataSetAutoSubmit($dataSetId: ID!, $autoSubmit: Boolean) {
              inbound {
                modifyDataSetAutoSubmit(dataSetId: $dataSetId, autoSubmit: $autoSubmit)
                __typename
              }
            }
            """;
            #endregion

            return $$"""
            {
              "operationName": "modifyDataSetAutoSubmit",
              "variables": {
                "dataSetId": "{{dataSetId}}",
                "autoSubmit": true
              },
              "query": {{GraphqlQueryHelper.Serialize(requestString)}}
            }
            """;
        }
    }
}
