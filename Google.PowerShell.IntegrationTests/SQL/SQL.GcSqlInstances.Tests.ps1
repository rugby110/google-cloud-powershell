﻿. $PSScriptRoot\..\GcloudCmdlets.ps1
Install-GcloudCmdlets
$project, $_, $oldActiveConfig, $configName = Set-GCloudConfig

Describe "Get-GcSqlInstance" {
    $r = Get-Random
    # A random number is used to avoid collisions with the speed of creating
    # and deleting instances.
    $instance = "test-inst$r"
    gcloud sql instances create $instance --quiet 2>$null

    It "should get a reasonable list response" {
        $instances = Get-GcSqlInstance -Project $project
        ($instances.Name -contains $instance) | Should Be true
    }

    It "shouldn't require the Project parameter to be specified" {
        $instances = Get-GcSqlInstance 
        ($instances.Name -contains $instance) | Should Be true
    }

    It "should be able to get information on a specific instance" {
        $testInstance = Get-GcSqlInstance $instance
        $testInstance.InstanceType | Should Be "CLOUD_SQL_INSTANCE"
        $testInstance.Name | Should Be $instance
    }

    It "should compound with the list parameter set" {
        $instances = Get-GcSqlInstance 
        $firstInstance = $instances | Select-Object -first 1
        $testInstance = Get-GcSqlInstance $firstInstance.Name
        $testInstance.Name | Should Be $firstInstance.Name
        $testInstance.SelfLink | Should be $firstInstance.SelfLink
    }

    It "should take in pipeline input" {
        $firstInstance = Get-GcSqlInstance | Select-Object -first 1
        $testInstance = Get-GcSqlInstance | Select-Object -first 1 | Get-GcSqlInstance
        $testInstance.Name | Should Be $firstInstance.Name
        $testInstance.SelfLink | Should be $firstInstance.SelfLink
    }
    
    gcloud sql instances delete $instance --quiet 2>$null
}

Describe "Add-GcSqlInstance" {

    It "should work" {
        $r = Get-Random
        # A random number is used to avoid collisions with the speed of creating
        # and deleting instances.
        $instance = "test-inst$r"
        $instances = Get-GcSqlInstance
        $setting = New-GcSqlSettingConfig "db-n1-standard-1"
        $config = New-GcSqlInstanceConfig $instance -SettingConfig $setting
        Add-GcSqlInstance $config
        $newInstances = Get-GcSqlInstance
        $newInstances.Count | Should Be ($instances.Count + 1)
        ($newInstances.Name -contains $instance) | Should Be true
        gcloud sql instances delete $instance --quiet 2>$null
    }
    
    It "should be able to reflect custom settings" {
        $r = Get-Random
        # A random number is used to avoid collisions with the speed of creating
        # and deleting instances.
        $instance = "test-inst$r"
        $setting = New-GcSqlSettingConfig "db-n1-standard-1" -MaintenanceWindowDay 1 -MaintenanceWindowHour 2
        $config = New-GcSqlInstanceConfig $instance -SettingConfig $setting
        Add-GcSqlInstance $config
        $myInstance = Get-GcSqlInstance $instance
        $myInstance.Settings.MaintenanceWindow.Day | Should Be 1
        $myInstance.Settings.MaintenanceWindow.Hour | Should Be 2
        gcloud sql instances delete $instance --quiet 2>$null
    }
}

Describe "Remove-GcSqlInstance" {
    It "should work" {
        $r = Get-Random
        # A random number is used to avoid collisions with the speed of creating
        # and deleting instances.
        $instance = "test-inst$r"
        gcloud sql instances create $instance --quiet 2>$null
        $instances = Get-GcSqlInstance
        ($instances.Name -contains $instance) | Should Be true
        Remove-GcSqlInstance $instance
        $instances = Get-GcSqlInstance
        ($instances.Name -contains $instance) | Should Be false
    }

    It "should be able to take a pipelined Instance" {
        $r = Get-Random
        # A random number is used to avoid collisions with the speed of creating
        # and deleting instances.
        $instance = "test-inst$r"
        gcloud sql instances create $instance --quiet 2>$null
        $instances = Get-GcSqlInstance
        ($instances.Name -contains $instance) | Should Be true
        Get-GcSqlInstance $instance | Remove-GcSqlInstance
        $instances = Get-GcSqlInstance
        ($instances.Name -contains $instance) | Should Be false
    }

    It "shouldn't delete anything that doesn't exist" {
        { Remove-GcSqlInstance "should-fail" } | Should Throw "The client is not authorized to make this request. [403]"
    }
}

Reset-GCloudConfig $oldActiveConfig $configName