﻿//
// UpdateAllNuGetPackagesInProjectAction.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

namespace MonoDevelop.PackageManagement
{
	internal class UpdateAllNuGetPackagesInProjectAction : IPackageAction
	{
		NuGetPackageManager packageManager;
		PackageRestoreManager restoreManager;
		IMonoDevelopSolutionManager solutionManager;
		IPackageManagementEvents packageManagementEvents;
		IDotNetProject dotNetProject;
		NuGetProject project;
		ISourceRepositoryProvider sourceRepositoryProvider;
		bool includePrerelease;
		string projectName;

		public UpdateAllNuGetPackagesInProjectAction (
			IMonoDevelopSolutionManager solutionManager,
			DotNetProject dotNetProject)
		{
			this.solutionManager = solutionManager;
			this.dotNetProject = new DotNetProjectProxy (dotNetProject);

			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			project = solutionManager.GetNuGetProject (new DotNetProjectProxy (dotNetProject));

			projectName = dotNetProject.Name;

			var restartManager = new DeleteOnRestartManager ();

			sourceRepositoryProvider = solutionManager.CreateSourceRepositoryProvider ();

			packageManager = new NuGetPackageManager (
				sourceRepositoryProvider,
				solutionManager.Settings,
				solutionManager,
				restartManager
			);

			restoreManager = new PackageRestoreManager (
				sourceRepositoryProvider,
				solutionManager.Settings,
				solutionManager
			);
		}

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			ExecuteAsync (cancellationToken).Wait ();
		}

		async Task ExecuteAsync (CancellationToken cancellationToken)
		{
			INuGetProjectContext context = CreateProjectContext ();

			includePrerelease = await ProjectHasPrereleasePackages (cancellationToken);

			await RestoreAnyMissingPackages (context, cancellationToken);

			var actions = await packageManager.PreviewUpdatePackagesAsync (
				project,
				CreateResolutionContext (),
				context,
				sourceRepositoryProvider.GetRepositories ().ToList (),
				new SourceRepository[0],
				cancellationToken);

			if (!actions.Any ()) {
				packageManagementEvents.OnNoUpdateFound (dotNetProject);
			}

			await packageManager.ExecuteNuGetProjectActionsAsync (
				project,
				actions,
				context,
				cancellationToken);
		}

		async Task<bool> ProjectHasPrereleasePackages (CancellationToken cancellationToken)
		{
			var packageReferences = await project.GetInstalledPackagesAsync (cancellationToken);
			return packageReferences.Any (packageReference => packageReference.PackageIdentity.Version.IsPrerelease);
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		ResolutionContext CreateResolutionContext ()
		{
			return new ResolutionContext (
				DependencyBehavior.Lowest,
				includePrerelease,
				false,
				VersionConstraints.None
			);
		}

		INuGetProjectContext CreateProjectContext ()
		{
			return new NuGetProjectContext (); 
		}

		async Task RestoreAnyMissingPackages (INuGetProjectContext context, CancellationToken cancellationToken)
		{
			var packages = await restoreManager.GetPackagesInSolutionAsync (
				solutionManager.SolutionDirectory,
				cancellationToken);

			var missingPackages = packages.Select (package => IsMissingForCurrentProject (package)).ToList ();
			if (missingPackages.Any ()) {
				using (var monitor = new PackageRestoreMonitor (restoreManager)) {
					await restoreManager.RestoreMissingPackagesAsync (
						solutionManager.SolutionDirectory,
						project,
						context,
						cancellationToken);
				}

				await Runtime.RunInMainThread (() => dotNetProject.RefreshReferenceStatus ());

				packageManagementEvents.OnPackagesRestored ();
			}
		}

		bool IsMissingForCurrentProject (PackageRestoreData package)
		{
			return package.IsMissing && package.ProjectNames.Any (name => name == projectName);
		}
	}
}

