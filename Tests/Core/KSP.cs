using System;
using System.IO;
using CKAN;
using CKAN.Versioning;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core
{
    [TestFixture]
    public class KSP
    {
        private CKAN.KSP ksp;
        private string ksp_dir;

        [SetUp]
        public void Setup()
        {
            ksp_dir = TestData.NewTempDir();
            TestData.CopyDirectory(TestData.good_ksp_dir(), ksp_dir);
            ksp = new CKAN.KSP(ksp_dir, "test", NullUser.User);
        }

        [TearDown]
        public void TearDown()
        {
            if (ksp != null)
            {
                // Manually dispose of RegistryManager
                // For some reason the KSP instance doesn't do this itself causing test failures because the registry
                // lock file is still in use. So just dispose of it ourselves.
                CKAN.RegistryManager.Instance(ksp).Dispose();
                ksp.Dispose();
            }

            Directory.Delete(ksp_dir, true);
        }

        [Test]
        public void IsGameDir()
        {
            // Our test data directory should be good.
            Assert.IsTrue(CKAN.KSP.IsKspDir(TestData.good_ksp_dir()));

            // As should our copied folder.
            Assert.IsTrue(CKAN.KSP.IsKspDir(ksp_dir));

            // And the one from our KSP instance.
            Assert.IsTrue(CKAN.KSP.IsKspDir(ksp.GameDir()));

            // All these ones should be bad.
            foreach (string dir in TestData.bad_ksp_dirs())
            {
                Assert.IsFalse(CKAN.KSP.IsKspDir(dir));
            }
        }

        [Test]
        public void Training()
        {
            //Use Uri to avoid issues with windows vs linux line seperators.
            var canonicalPath = new Uri(Path.Combine(ksp_dir, "saves", "training")).LocalPath;
            var training = new Uri(ksp.Tutorial()).LocalPath;
            Assert.AreEqual(canonicalPath, training);
        }

        [Test]
        public void ScanDlls()
        {
            string path = Path.Combine(ksp.GameData(), "Example.dll");
            var registry = CKAN.RegistryManager.Instance(ksp).registry;

            Assert.IsFalse(registry.IsInstalled("Example"), "Example should start uninstalled");

            File.WriteAllText(path, "Not really a DLL, are we?");

            ksp.ScanGameData();

            Assert.IsTrue(registry.IsInstalled("Example"), "Example installed");

            ModuleVersion version = registry.InstalledVersion("Example");
            Assert.IsInstanceOf<UnmanagedModuleVersion>(version, "DLL detected as a DLL, not full mod");

            // Now let's do the same with different case.

            string path2 = Path.Combine(ksp.GameData(), "NewMod.DLL");

            Assert.IsFalse(registry.IsInstalled("NewMod"));
            File.WriteAllText(path2, "This text is irrelevant. You will be assimilated");

            ksp.ScanGameData();

            Assert.IsTrue(registry.IsInstalled("NewMod"));
        }

        [Test]
        public void ToAbsolute()
        {
            Assert.AreEqual(
                CKAN.KSPPathUtils.NormalizePath(
                    Path.Combine(ksp_dir, "GameData/HydrazinePrincess")
                ),
                ksp.ToAbsoluteGameDir("GameData/HydrazinePrincess")
            );
        }

        [Test]
        public void ToRelative()
        {
            string absolute = Path.Combine(ksp_dir, "GameData/HydrazinePrincess");

            Assert.AreEqual(
                "GameData/HydrazinePrincess",
                ksp.ToRelativeGameDir(absolute)
            );
        }

    }
}
