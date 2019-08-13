﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using System.Diagnostics;
using AzureServiceCatalog.Models;
using AzureServiceCatalog.Helpers;
using AzureServiceCatalog.Web.Models;
using Newtonsoft.Json.Linq;

namespace AzureServiceCatalog.Web.Controllers
{
    [RoutePrefix("api/identity")]
    public class IdentityController : ApiController
    {
        private TableCoreRepository coreRepository = new TableCoreRepository();
        [Route("full")]
        public async Task<IHttpActionResult> GetUserDetailsFull()
        {
            try
            {
                var subscriptionInfo = new UserSubscriptionInfo();
                subscriptionInfo.UserName = ClaimsPrincipal.Current.Identity.Name;
                subscriptionInfo.FirstName = ClaimsPrincipal.Current.FirstName();
                subscriptionInfo.LastName = ClaimsPrincipal.Current.LastName();
                subscriptionInfo.DefaultAdGroup = Config.DefaultAdGroup;
                subscriptionInfo.DefaultResourceGroup = Config.DefaultResourceGroup;
                string tenantId = ClaimsPrincipal.Current.TenantId();
                string signedInUserUniqueId = ClaimsPrincipal.Current.SignedInUserName();

                var userGroupsRoles = await AzureADGraphApiUtil.GetUserGroups(signedInUserUniqueId, tenantId);
                subscriptionInfo.IsGlobalAdministrator = AzureADGraphApiUtil.IsGlobalAdministrator(userGroupsRoles);

                var org = await GetOrganization(tenantId);
                var dbOrg = await this.coreRepository.GetOrganization(tenantId);
                List<Subscription> dbSubscriptions = null;

                if (dbOrg != null)
                {
                    org.DeployGroup = dbOrg.DeployGroup;
                    org.CreateProductGroup = dbOrg.CreateProductGroup;
                    org.AdminGroup = dbOrg.AdminGroup;
                    //var userGroups = await AzureADGraphApiUtil.GetUserGroups(signedInUserUniqueId, org.Id);
                    subscriptionInfo.CanCreate = userGroupsRoles.Any(x => x.Id == dbOrg.CreateProductGroup);
                    subscriptionInfo.CanDeploy = userGroupsRoles.Any(x => x.Id == dbOrg.DeployGroup);
                    subscriptionInfo.CanAdmin = userGroupsRoles.Any(x => x.Id == dbOrg.AdminGroup);
                    dbSubscriptions = this.coreRepository.GetSubscriptionListByOrgId(tenantId);

                    if (dbSubscriptions != null && dbSubscriptions.Count > 0)
                    {
                        subscriptionInfo.IsActivatedByAdmin = (dbSubscriptions.Any(x => x.IsConnected));
                    }
                }

                subscriptionInfo.Organization = org;

                var orgGroups = await AzureADGraphApiUtil.GetAllGroupsForOrganization(org.Id);
                subscriptionInfo.OrganizationADGroups = orgGroups;
                var subscriptions = await AzureResourceManagerUtil.GetUserSubscriptions(org.Id);
                if (subscriptions != null)
                {
                    foreach (var subscription in subscriptions)
                    {
                        var userDetailVM = new UserDetailsViewModel();
                        userDetailVM.CanCreate = subscriptionInfo.CanCreate;
                        userDetailVM.CanDeploy = subscriptionInfo.CanDeploy;
                        userDetailVM.CanAdmin = subscriptionInfo.CanAdmin;
                        userDetailVM.Name = subscriptionInfo.UserName;

                        userDetailVM.IsAdminOfSubscription = await AzureResourceManagerUtil.UserCanManageAccessForSubscription(subscription.Id);
                        userDetailVM.SubscriptionName = subscription.DisplayName;
                        userDetailVM.SubscriptionId = subscription.Id;
                        userDetailVM.OrganizationId = org.Id;
                        userDetailVM.ServicePrincipalId = org.ObjectIdOfCloudSenseServicePrincipal;
                        userDetailVM.OrganizationName = org.DisplayName;

                        Subscription dbSubscription = null;
                        if (dbSubscriptions != null && dbSubscriptions.Count > 0)
                        {
                            dbSubscription = dbSubscriptions.FirstOrDefault(x => x.Id == subscription.Id) ?? null;
                        }

                        if (dbSubscription != null)
                        {
                            userDetailVM.SubscriptionIsConnected = dbSubscription.IsConnected;// true;
                            userDetailVM.IsEnrolled = dbSubscription.IsEnrolled;
                            userDetailVM.SubscriptionNeedsRepair = !await AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(dbSubscription.Id);
                            if (userDetailVM.SubscriptionIsConnected)
                            {
                                string organizationId = dbSubscription.OrganizationId;
                                string storageName = dbSubscription.StorageName;
                                try
                                {
                                    string storageKey = await AzureResourceManagerUtil.GetStorageAccountKeysArm(dbSubscription.Id, dbSubscription.StorageName);
                                    CacheDetails(userDetailVM, storageKey, storageName, organizationId, signedInUserUniqueId);
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError(ex.Message);
                                    Trace.TraceError($"Storage account: {storageName} was not found!");
                                    userDetailVM.SubscriptionIsConnected = false;
                                    userDetailVM.IsEnrolled = false;
                                }

                            }
                        }
                        subscriptionInfo.Subscriptions.Add(userDetailVM);
                    }
                }
                return this.Ok(subscriptionInfo);
            }
            catch (Exception)
            {
                return Content(HttpStatusCode.InternalServerError, JObject.FromObject(ErrorInformation.GetInternalServerErrorInformation()));
            }
            finally
            {

            }
        }

        [Route("")]
        public async Task<IHttpActionResult> GetUserDetails()
        {
            try
            {
                var subscriptionInfo = new UserSubscriptionInfo();
                subscriptionInfo.UserName = ClaimsPrincipal.Current.Identity.Name;
                var tenantId = ClaimsPrincipal.Current.TenantId();
                var signedInUserUniqueId = ClaimsPrincipal.Current.SignedInUserName();

                var org = await this.coreRepository.GetOrganization(tenantId);
                subscriptionInfo.Organization = org;

                var userGroups = await AzureADGraphApiUtil.GetUserGroups(signedInUserUniqueId, org.Id);
                subscriptionInfo.CanCreate = userGroups.Any(x => x.Id == org.CreateProductGroup);
                subscriptionInfo.CanDeploy = userGroups.Any(x => x.Id == org.DeployGroup);
                subscriptionInfo.CanAdmin = userGroups.Any(x => x.Id == org.AdminGroup);

                return this.Ok(subscriptionInfo);
            }
            catch (Exception)
            {
                return Content(HttpStatusCode.InternalServerError, JObject.FromObject(ErrorInformation.GetInternalServerErrorInformation()));
            }
            finally
            {

            }
        }

        [Route("organization-groups")]
        public async Task<IHttpActionResult> GetOrganizationGroups(string filter)
        {
            try
            {
                string tenantId = ClaimsPrincipal.Current.TenantId();
                var orgGroups = await AzureADGraphApiUtil.GetAllGroupsForOrganization(tenantId, filter);
                return this.Ok(orgGroups);
            }
            catch (Exception)
            {
                return Content(HttpStatusCode.InternalServerError, JObject.FromObject(ErrorInformation.GetInternalServerErrorInformation()));
            }
            finally
            {

            }
        }

        [Route("asc-contributor")]
        public async Task<IHttpActionResult> PutAscContributorRole(string subscriptionId)
        {
            try
            {
                var rbacClient = new RbacClient();
                //var json = await rbacClient.CreateAscContributorRoleOnSubscription(subscriptionId);

                //dynamic role = await rbacClient.GetAscContributorRole(subscriptionId);
                //var json = await rbacClient.DeleteCustomRoleOnSubscription(subscriptionId, (string)role.name);
                var json = await rbacClient.GetRoleAssignmentsForAscContributor(subscriptionId);

                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = json.ToStringContent();
                return this.Ok(response);
            }
            catch (Exception)
            {
                return Content(HttpStatusCode.InternalServerError, JObject.FromObject(ErrorInformation.GetInternalServerErrorInformation()));
            }
            finally
            {

            }
        }

        #region Private Methods

        private static async Task<Organization> GetOrganization(string orgId)
        {
            var organization = await AzureADGraphApiUtil.GetOrganizationDetails(orgId);
            var spid = await AzureADGraphApiUtil.GetObjectIdOfServicePrincipalInOrganization(orgId, Config.ClientId);
            organization.ObjectIdOfCloudSenseServicePrincipal = spid;
            return organization;
        }

        private static void CacheDetails(UserDetailsViewModel userDetailViewModel, string storageKey, string storageName, string organizationId, string signedInUserUniqueId)
        {
            if (!string.IsNullOrEmpty(storageName) && !string.IsNullOrEmpty(storageKey))
            {
                CacheUserDetails cud = new CacheUserDetails();
                cud.SubscriptionId = userDetailViewModel.SubscriptionId;
                cud.StorageName = storageName;
                cud.OrganizationId = organizationId;
                cud.StorageKey = storageKey;
                MemoryCacher.Delete(signedInUserUniqueId);
                MemoryCacher.Add(signedInUserUniqueId, cud, DateTime.Now.AddMinutes(15));
            }
        }
        #endregion
    }
}