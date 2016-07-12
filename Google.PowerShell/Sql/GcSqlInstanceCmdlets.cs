﻿// Copyright 2015-2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.SQLAdmin.v1beta4;
using Google.Apis.SQLAdmin.v1beta4.Data;
using System.Management.Automation;
using Google.PowerShell.Common;
using System.Collections.Generic;
using System.Linq;

namespace Google.PowerShell.Sql
{
    /// <summary>
    /// <para type="synopsis">
    /// Retrieves a resource containing information about a Cloud SQL instance, or lists all instances in a project.
    /// </para>
    /// <para type="description">
    /// Retrieves a resource containing the information for the specified Cloud SQL instance, 
    /// or lists all instances in a project.
    /// This is determined by if Instance is specified or not.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "GcSqlInstance")]
    public class GetGcSqlInstanceCmdlet : GcSqlCmdlet
    {
        internal class ParameterSetNames
        {
            public const string GetSingle = "Single";
            public const string GetList = "List";
        }

        /// <summary>
        /// <para type="description">
        /// Project name of the project that contains instance(s).
        /// Defaults to the cloud sdk config for properties if not specified.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetNames.GetSingle)]
        [Parameter(ParameterSetName = ParameterSetNames.GetList)]
        [ConfigPropertyName(CloudSdkSettings.CommonProperties.Project)]
        public string Project { get; set; }

        /// <summary>
        /// <para type="description">
        /// Cloud SQL instance name. 
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSetNames.GetSingle,
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (Name != null) {
                InstancesResource.GetRequest request = Service.Instances.Get(Project, Name);
                DatabaseInstance result = request.Execute();
                WriteObject(result);
            }
            else
            {
                IEnumerable<DatabaseInstance> results = GetAllSqlInstances();
                WriteObject(results, true);
            }
        }

        private IEnumerable<DatabaseInstance> GetAllSqlInstances()
        {
            InstancesResource.ListRequest request = Service.Instances.List(Project);
            do
            {
                InstancesListResponse aggList = request.Execute();
                IList<DatabaseInstance> instanceList = aggList.Items;
                if (instanceList == null)
                {
                    yield break;
                }
                foreach (DatabaseInstance instance in instanceList)
                {
                    yield return instance;
                }
                request.PageToken = aggList.NextPageToken;
            }
            while (request.PageToken != null);
        }
    }

    /// <summary>
    /// <para type="synopsis">
    /// Creates a new Cloud SQL instance.
    /// </para>
    /// <para type="description">
    /// Creates the Cloud SQL instance resource in the specified project.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "GcSqlInstance")]
    public class AddGcSqlInstanceCmdlet : GcSqlCmdlet
    {
        /// <summary>
        /// <para type="description">
        /// Name of the project.
        /// Defaults to the cloud sdk config for properties if not specified.
        /// </para>
        /// </summary>
        [Parameter]
        [ConfigPropertyName(CloudSdkSettings.CommonProperties.Project)]
        public string Project { get; set; }

        /// <summary>
        /// <para type="description">
        /// The instance resource. 
        /// Can be created with New-GcSqlInstanceConfig.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public DatabaseInstance InstanceConfig { get; set; }

        protected override void ProcessRecord()
        {
            InstancesResource.InsertRequest request = Service.Instances.Insert(InstanceConfig, Project);
            Operation result = request.Execute();
            WaitForSqlOperation(result);
            /// We get the instance that was just added
            /// so that the returned DatabaseInstance is as accurate as possible.
            InstancesResource.GetRequest instanceRequest = Service.Instances.Get(Project, InstanceConfig.Name);
            WriteObject(instanceRequest.Execute());
        }
    }

    /// <summary>
    /// <para type="synopsis">
    /// Deletes a Cloud SQL instance.
    /// </para>
    /// <para type="description">
    /// Deletes the specified Cloud SQL instance.
    /// 
    /// Warning: This deletes all data inside of it as well.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "GcSqlInstance", SupportsShouldProcess = true,
        DefaultParameterSetName = ParameterSetNames.ByName)]
    public class RemoveGcSqlInstanceCmdlet : GcSqlCmdlet
    {
        private class ParameterSetNames
        {
            public const string ByName = "ByName";
            public const string ByInstance = "ByInstance";
        }

        /// <summary>
        /// <para type="description">
        /// Name of the project.
        /// Defaults to the cloud sdk config for properties if not specified.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetNames.ByName)]
        [ConfigPropertyName(CloudSdkSettings.CommonProperties.Project)]
        public string Project { get; set; }

        /// <summary>
        /// <para type="description">
        /// The name of the instance to be deleted.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
            ParameterSetName = ParameterSetNames.ByName)]
        public string Instance { get; set; }

        /// <summary>
        /// <para type="description">
        /// The DatabaseInstance that describes the instance we want to remove.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetNames.ByInstance, Mandatory = true,
            Position = 0, ValueFromPipeline = true)]
        public DatabaseInstance InstanceObject { get; set; }

        protected override void ProcessRecord()
        {
            string project;
            string instance;
            switch (ParameterSetName)
            {
                case ParameterSetNames.ByName:
                    instance = Instance;
                    project = Project;
                    break;
                case ParameterSetNames.ByInstance:
                    instance = InstanceObject.Name;
                    project = InstanceObject.Project;
                    break;
                default:
                    throw UnknownParameterSetException;
            }
            if (!ShouldProcess($"{project}/{instance}", "Delete Instance"))
            {
                return;
            }
            InstancesResource.DeleteRequest request = Service.Instances.Delete(project, instance);
            Operation result = request.Execute();
            WaitForSqlOperation(result);
        }
    }
}