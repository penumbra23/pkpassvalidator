﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography.Pkcs;
using System.Text;

namespace PassValidator.Lib.Validation
{
    public class Validator
    {
        public ValidationResult Validate(byte[] passContent)
        {
            ValidationResult result = new ValidationResult();

            string passTypeIdentifier = null;
            string teamIdentifier = null;
            string signaturePassTypeIdentifier = null;
            string signatureTeamIdentifier = null;
            byte[] manifestFile = null;
            byte[] signatureFile = null;

            using (MemoryStream zipToOpen = new MemoryStream(passContent))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read, false))
                {
                    foreach (var e in archive.Entries)
                    {
                        if (e.FullName.ToLower().Equals("manifest.json"))
                        {
                            result.HasManifest = true;

                            using (var stream = e.Open())
                            {
                                using (var ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    ms.Position = 0;
                                    manifestFile = ms.ToArray();
                                }
                            }
                        }

                        if (e.FullName.ToLower().Equals("pass.json"))
                        {
                            result.HasPass = true;

                            using (var stream = e.Open())
                            {
                                using (var ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    ms.Position = 0;
                                    var file = ms.ToArray();

                                    var jsonObject = JObject.Parse(Encoding.UTF8.GetString(file));

                                    passTypeIdentifier = GetKeyStringValue(jsonObject, "passTypeIdentifier");
                                    result.HasPassTypeIdentifier = !string.IsNullOrWhiteSpace(passTypeIdentifier);

                                    teamIdentifier = GetKeyStringValue(jsonObject, "teamIdentifier");
                                    result.HasTeamIdentifier = !string.IsNullOrWhiteSpace(teamIdentifier);

                                    var description = GetKeyStringValue(jsonObject, "description");
                                    result.HasDescription = !string.IsNullOrWhiteSpace(description);

                                    if (jsonObject.ContainsKey("formatVersion"))
                                    {
                                        var formatVersion = jsonObject["formatVersion"].Value<int>();
                                        result.HasFormatVersion = formatVersion == 1;
                                    }

                                    var serialNumber = GetKeyStringValue(jsonObject, "serialNumber");
                                    result.HasSerialNumber = !string.IsNullOrWhiteSpace(serialNumber);

                                    if (result.HasSerialNumber)
                                    {
                                        result.hasSerialNumberOfCorrectLength = serialNumber.Length >= 16;
                                    }

                                    var organizationName = GetKeyStringValue(jsonObject, "organizationName");
                                    result.HasOrganizationName = !string.IsNullOrWhiteSpace(organizationName);

                                    if (jsonObject.ContainsKey("appLaunchURL"))
                                    {
                                        result.HasAppLaunchUrl = true;
                                        result.HasAssociatedStoreIdentifiers = jsonObject.ContainsKey("associatedStoreIdentifiers");
                                    }

                                    if (jsonObject.ContainsKey("webServiceURL"))
                                    {
                                        result.HasWebServiceUrl = true;

                                        var webServiceUrl = GetKeyStringValue(jsonObject, "webServiceURL");
                                        result.WebServiceUrlIsHttps = webServiceUrl.ToLower().StartsWith("https://");
                                    }

                                    if (jsonObject.ContainsKey("authenticationToken"))
                                    {
                                        result.HasAuthenticationToken = true;

                                        var authenticationToken = GetKeyStringValue(jsonObject, "authenticationToken");
                                        result.AuthenticationTokenIsCorrectLength = authenticationToken.Length >= 16;
                                    }
                                }
                            }
                        }

                        if (e.FullName.ToLower().Equals("signature"))
                        {
                            result.HasSignature = true;

                            using (var stream = e.Open())
                            {
                                using (var ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    ms.Position = 0;
                                    signatureFile = ms.ToArray();
                                }

                            }
                        }

                        if (e.FullName.ToLower().Equals("icon.png"))
                        {
                            result.HasIcon1x = true;
                        }

                        if (e.FullName.ToLower().Equals("icon@2x.png"))
                        {
                            result.HasIcon2x = true;
                        }

                        if (e.FullName.ToLower().Equals("icon@3x.png"))
                        {
                            result.HasIcon3x = true;
                        }
                    }
                }
            }

            if (result.HasManifest)
            {
                ContentInfo contentInfo = new ContentInfo(manifestFile);
                SignedCms signedCms = new SignedCms(contentInfo, true);

                signedCms.Decode(signatureFile);

                try
                {
                    signedCms.CheckSignature(true);
                }
                catch
                {

                }

                var signer = signedCms.SignerInfos[0];

                var wwdrCertSubject = "CN=Apple Worldwide Developer Relations Certification Authority, OU=Apple Worldwide Developer Relations, O=Apple Inc., C=US";

                var appleWWDRCertificate = signedCms.Certificates[0];

                result.WWDRCertificateExpired = appleWWDRCertificate.NotAfter < DateTime.UtcNow;
                result.WWDRCertificateSubjectMatches = appleWWDRCertificate.Issuer == wwdrCertSubject;

                result.SignedByApple = signer.Certificate.IssuerName.Name == wwdrCertSubject;


                if (result.SignedByApple)
                {
                    var cnValues = Parse(signer.Certificate.Subject, "CN");
                    var ouValues = Parse(signer.Certificate.Subject, "OU");

                    var passTypeIdentifierSubject = cnValues[0];
                    signaturePassTypeIdentifier = passTypeIdentifierSubject.Replace("Pass Type ID: ", "");

                    if (ouValues != null && ouValues.Count > 0)
                    {
                        signatureTeamIdentifier = ouValues[0];
                    }

                    result.HasSignatureExpired = signer.Certificate.NotAfter < DateTime.UtcNow;
                    result.SignatureExpirationDate = signer.Certificate.NotAfter.ToString("yyyy-MM-dd HH:mm:ss");
                }

                result.PassTypeIdentifierMatches = passTypeIdentifier == signaturePassTypeIdentifier;
                result.TeamIdentifierMatches = teamIdentifier == signatureTeamIdentifier;
            }

            return result;
        }

        private string GetKeyStringValue(JObject jsonObject, string key)
        {
            return jsonObject.ContainsKey(key) ? jsonObject[key].Value<string>() : null;
        }

        public static List<string> Parse(string data, string delimiter)
        {
            if (data == null) return null;
            if (!delimiter.EndsWith("=")) delimiter = delimiter + "=";
            if (!data.Contains(delimiter)) return null;
            //base case
            var result = new List<string>();
            int start = data.IndexOf(delimiter) + delimiter.Length;
            int length = data.IndexOf(',', start) - start;
            if (length == 0) return null; //the group is empty
            if (length > 0)
            {
                result.Add(data.Substring(start, length));
                //only need to recurse when the comma was found, because there could be more groups
                var rec = Parse(data.Substring(start + length), delimiter);
                if (rec != null) result.AddRange(rec); //can't pass null into AddRange() :(
            }
            else //no comma found after current group so just use the whole remaining string
            {
                result.Add(data.Substring(start));
            }
            return result;
        }
    }
}
