﻿// 
// PackageManagementOptions.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using MonoDevelop.Core;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementOptions
	{
		const string PackageDirectoryPropertyName = "PackagesDirectory";
		const string RecentPackagesPropertyName = "RecentPackages";
		const string AutomaticPackageRestoreOnOpeningSolutionPropertyName = "AutomaticPackageRestoreOnOpeningSolution";
		const string CheckUpdatedPackagesOnOpeningSolutionPropertyName = "CheckUpdatedPackagesOnOpeningSolution";

		RegisteredPackageSourceSettings registeredPackageSourceSettings;
		Properties properties;

		public PackageManagementOptions (
			Properties properties,
			ISettingsProvider settingsProvider)
		{
			this.properties = properties;
			registeredPackageSourceSettings = new RegisteredPackageSourceSettings (settingsProvider);
		}

		public PackageManagementOptions (Properties properties)
			: this (properties, new SettingsProvider ())
		{
		}

		public PackageManagementOptions()
			: this(PropertyService.Get("PackageManagementSettings", new Properties()))
		{
		}

		public bool IsAutomaticPackageRestoreOnOpeningSolutionEnabled {
			get { return properties.Get(AutomaticPackageRestoreOnOpeningSolutionPropertyName, true); }
			set { properties.Set(AutomaticPackageRestoreOnOpeningSolutionPropertyName, value); }
		}

		public bool IsCheckForPackageUpdatesOnOpeningSolutionEnabled {
			get { return properties.Get(CheckUpdatedPackagesOnOpeningSolutionPropertyName, true); }
			set { properties.Set(CheckUpdatedPackagesOnOpeningSolutionPropertyName, value); }
		}
		
		public RegisteredPackageSources PackageSources {
			get { return registeredPackageSourceSettings.PackageSources; }
		}
		
		public PackageSource ActivePackageSource {
			get { return registeredPackageSourceSettings.ActivePackageSource; }
			set { registeredPackageSourceSettings.ActivePackageSource = value; }
		}

		public string GetCustomPackagesDirectory ()
		{
			return registeredPackageSourceSettings.Settings.GetRepositoryPath ();
		}
	}
}
