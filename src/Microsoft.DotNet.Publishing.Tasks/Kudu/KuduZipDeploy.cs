﻿using System;
using System.IO;
using System.Net.Http;
using Microsoft.Build.Utilities;

namespace Microsoft.DotNet.Publishing.Tasks.Kudu
{
    public class KuduZipDeploy: KuduConnect
    {
        private TaskLoggingHelper _logger;

        public KuduZipDeploy(KuduConnectionInfo connectionInfo, TaskLoggingHelper logger)
            : base(connectionInfo, logger)
        {
            _logger = logger;
        }

        public override string DestinationUrl
        {
            get
            {
                return String.Format(ConnectionInfo.DestinationUrl, ConnectionInfo.SiteName, "zip/site/wwwroot/");
            }
        }

        public async System.Threading.Tasks.Task<bool> DeployAsync(string zipFileFullPath)
        {

            if (!File.Exists(zipFileFullPath))
            {
                // If the source file directory does not exist quit early.
                _logger.LogError(String.Format(SR.KUDUDEPLOY_AzurePublishErrorReason, SR.KUDUDEPLOY_DeployOutputPathEmpty));
                return false;
            }

            var success = await PostZipAsync(zipFileFullPath);
            return success;
        }

        private async System.Threading.Tasks.Task<bool> PostZipAsync(string zipFilePath)
        {
            if (String.IsNullOrEmpty(zipFilePath))
            {
                return false;
            }

            using (var client = new HttpClient())
            {
                using (var content = new StreamContent(File.OpenRead(zipFilePath)))
                {
                    content.Headers.Remove("Content-Type");
                    content.Headers.Add("Content-Type", "application/octet-stream");

                    using (var req = new HttpRequestMessage(HttpMethod.Put, DestinationUrl))
                    {
                        req.Headers.Add("Authorization", "Basic " + AuthorizationInfo);
                        req.Content = content;

                        _logger.LogMessage(Microsoft.Build.Framework.MessageImportance.High, SR.KUDUDEPLOY_PublishAzure);
                        using (var response = await client.SendAsync(req))
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogError(String.Format(SR.KUDUDEPLOY_PublishZipFailedReason, ConnectionInfo.SiteName, response.ReasonPhrase));
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}
