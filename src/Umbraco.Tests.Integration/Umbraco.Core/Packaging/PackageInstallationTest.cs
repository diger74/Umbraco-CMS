// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models.Packaging;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Cms.Tests.Integration.Umbraco.Core.Packaging
{
    [TestFixture]
    [UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture)]
    public class PackageInstallationTest : UmbracoIntegrationTest
    {
        private IHostingEnvironment HostingEnvironment => GetRequiredService<IHostingEnvironment>();

        private IPackageInstallation PackageInstallation => GetRequiredService<IPackageInstallation>();

        private const string DocumentTypePickerPackage = "Document_Type_Picker_1.1.umb";
        private const string HelloPackage = "Hello_1.0.0.zip";

        [Test]
        public void Can_Read_Compiled_Package_1()
        {
            var testPackageFile = new FileInfo(Path.Combine(HostingEnvironment.MapPathContentRoot("~/TestData/Packages"), DocumentTypePickerPackage));
            CompiledPackage package = PackageInstallation.ReadPackage(testPackageFile);
            Assert.IsNotNull(package);
            Assert.AreEqual(1, package.Files.Count);
            Assert.AreEqual("095e064b-ba4d-442d-9006-3050983c13d8.dll", package.Files[0].UniqueFileName);
            Assert.AreEqual("/bin", package.Files[0].OriginalPath);
            Assert.AreEqual("Auros.DocumentTypePicker.dll", package.Files[0].OriginalName);
            Assert.AreEqual("Document Type Picker", package.Name);
            Assert.AreEqual(RequirementsType.Legacy, package.UmbracoVersionRequirementsType);
            Assert.AreEqual(1, package.DataTypes.Count());
        }

        [Test]
        public void Can_Read_Compiled_Package_2()
        {
            var testPackageFile = new FileInfo(Path.Combine(HostingEnvironment.MapPathContentRoot("~/TestData/Packages"), HelloPackage));
            CompiledPackage package = PackageInstallation.ReadPackage(testPackageFile);
            Assert.IsNotNull(package);
            Assert.AreEqual(0, package.Files.Count);
            Assert.AreEqual("Hello", package.Name);
            Assert.AreEqual(RequirementsType.Strict, package.UmbracoVersionRequirementsType);
            Assert.AreEqual(1, package.Documents.Count());
            Assert.AreEqual(1, package.DocumentTypes.Count());
            Assert.AreEqual(1, package.Templates.Count());
            Assert.AreEqual(1, package.DataTypes.Count());
        }

        [Test]
        public void Can_Read_Compiled_Package_Warnings()
        {
            // Copy a file to the same path that the package will install so we can detect file conflicts.
            string filePath = Path.Combine(HostingEnvironment.MapPathContentRoot("~/"), "bin", "Auros.DocumentTypePicker.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, "test");

            // this is where our test zip file is
            string packageFile = Path.Combine(HostingEnvironment.MapPathContentRoot("~/TestData/Packages"), DocumentTypePickerPackage);
            Console.WriteLine(packageFile);

            CompiledPackage package = PackageInstallation.ReadPackage(new FileInfo(packageFile));
            InstallWarnings preInstallWarnings = package.Warnings;
            Assert.IsNotNull(preInstallWarnings);

            Assert.AreEqual(1, preInstallWarnings.FilesReplaced.Count());
            Assert.AreEqual(Path.Combine("bin", "Auros.DocumentTypePicker.dll"), preInstallWarnings.FilesReplaced.First());

            // TODO: More Asserts
        }

        [Test]
        public void Install_Data()
        {
            var testPackageFile = new FileInfo(Path.Combine(HostingEnvironment.MapPathContentRoot("~/TestData/Packages"), DocumentTypePickerPackage));
            CompiledPackage package = PackageInstallation.ReadPackage(testPackageFile);
            var def = PackageDefinition.FromCompiledPackage(package);
            def.Id = 1;
            def.PackageId = Guid.NewGuid();

            InstallationSummary summary = PackageInstallation.InstallPackageData(def, package, -1);

            Assert.AreEqual(1, summary.DataTypesInstalled.Count());

            // make sure the def is updated too
            Assert.AreEqual(summary.DataTypesInstalled.Count(), def.DataTypes.Count);
        }
    }
}
