﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Abstractions.TestingHelpers;
using System.Collections.Generic;
using AzureSiteReplicator.Contracts;
using Moq;
using System.IO;
using AzureSiteReplicator.Data;
using System.Linq;


namespace AzureSiteReplicator.Test
{
    [TestClass]
    public class ConfigTests
    {
        private Mock<IEnvironment> _mockEnv = null;

        [TestInitialize]
        public void Setup()
        {
            _mockEnv = new Mock<IEnvironment>();
            _mockEnv.Setup(m => m.SiteReplicatorPath).Returns(@"c:\");
            
            Environment.Instance = _mockEnv.Object;
            FileHelper.FileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());

        }

        [TestMethod]
        public void LoadOrCreateConfigTest()
        {
            ConfigFile config = null;

            List<string> dirs = new List<string>()
            {
                @"c:\test1",
                @"c:\test2",
                @"c:\test3",
                @"c:\test4"
            };
            
            Dictionary<string, MockFileData> tests = new Dictionary<string, MockFileData>();
            tests.Add(Path.Combine(dirs[0], "config.xml"),
                new MockFileData(
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<config>" +
                    "</config>"));

            tests.Add(Path.Combine(dirs[1], "config.xml"),
                new MockFileData(
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<config>" +
                    "  <skipFiles>" +
                    "    <skip></skip>" +
                    "  </skipFiles>" +
                    "</config>"));

            tests.Add(Path.Combine(dirs[2], "config.xml"),
                new MockFileData(
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<config>" +
                    "  <skipFiles>" +
                    "    <skip>skip1</skip>" +
                    "  </skipFiles>" +
                    "</config>"));

            tests.Add(Path.Combine(dirs[3], "config.xml"),
                new MockFileData(
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<config>" +
                    "  <skipFiles>" +
                    "    <skip>skip1</skip>" +
                    "    <skip>skip2</skip>" +
                    "  </skipFiles>" +
                    "</config>"));

            var expected = new[]{
                new List<string>(),
                new List<string>(){""},
                new List<string>(){"skip1"},
                new List<string>(){"skip1","skip2"}
            };

            FileHelper.FileSystem = new MockFileSystem(tests);

            for(int i = 0; i < dirs.Count; i++)
            {
                _mockEnv.Setup(m => m.SiteReplicatorPath).Returns(dirs[i]);
                config = new ConfigFile();
                config.LoadOrCreate();

                TestHelpers.VerifyEnumerable<string>(
                    expected[i].AsEnumerable(),
                    config.SkipFiles);
            }

            // Test file creation
            _mockEnv.Setup(m => m.SiteReplicatorPath).Returns(@"c:\foo");
            config = new ConfigFile();
            config.LoadOrCreate();

            Assert.IsTrue(FileHelper.FileSystem.File.Exists(@"c:\foo\config.xml"), "Didn't create a new config file");
            TestHelpers.VerifyEnumerable<string>((new List<string>()).AsEnumerable(), config.SkipFiles);
        }

        [TestMethod]
        public void SaveConfigTest()
        {
            var tests = new[]{
                new List<string>(),
                new List<string>(){"skip1"},
                new List<string>(){"skip1","skip2"}
            };

            System.IO.Abstractions.FileBase MockFile = FileHelper.FileSystem.File;

            for (int i = 0; i < tests.Length; i++)
            {
                ConfigFile config = new ConfigFile();
                config.SetSkips(tests[i]);
                config.Save();

                Assert.IsTrue(MockFile.Exists(@"c:\config.xml"));

                config = new ConfigFile();
                config.LoadOrCreate();

                TestHelpers.VerifyEnumerable<string>(tests[i].AsEnumerable(), config.SkipFiles);
            }
        }
    }
}
