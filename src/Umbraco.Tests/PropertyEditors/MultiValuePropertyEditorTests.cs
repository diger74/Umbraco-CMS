﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Moq;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Profiling;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Tests.TestHelpers;
using Umbraco.Web.PropertyEditors;

namespace Umbraco.Tests.PropertyEditors
{
    /// <summary>
    /// Tests for the base classes of ValueEditors and PreValueEditors that are used for Property Editors that edit
    /// multiple values such as the drop down list, check box list, color picker, etc....
    /// </summary>
    [TestFixture]
    public class MultiValuePropertyEditorTests
    {
        //TODO: Test the other formatting methods for the drop down classes

        [Test]
        public void DropDownMultipleValueEditor_With_Keys_Format_Data_For_Cache()
        {
            var dataTypeServiceMock = new Mock<IDataTypeService>();
            var editor = new PublishValuesMultipleValueEditor(true, Mock.Of<ILogger>(), new ValueEditorAttribute("key", "nam", "view"));

            var prop = new Property(1, new PropertyType(new DataType(1, "Test.TestEditor")));
            prop.SetValue("1234,4567,8910");

            var result = editor.ConvertDbToString(prop.PropertyType, prop.GetValue(), new Mock<IDataTypeService>().Object);

            Assert.AreEqual("1234,4567,8910", result);
        }

        [Test]
        public void DropDownMultipleValueEditor_No_Keys_Format_Data_For_Cache()
        {
            var dataType = new DataType("editorAlias")
            {
                Configuration = new ValueListConfiguration
                {
                    Items = new List<ValueListConfiguration.ValueListItem>
                    {
                        new ValueListConfiguration.ValueListItem { Id = 4567, Value = "Value 1" },
                        new ValueListConfiguration.ValueListItem { Id = 1234, Value = "Value 2" },
                        new ValueListConfiguration.ValueListItem { Id = 8910, Value = "Value 3" }
                    }
                }
            };

            var dataTypeService = Mock.Of<IDataTypeService>();

            Mock.Get(dataTypeService)
                .Setup(x => x.GetDataType(It.IsAny<int>()))
                .Returns<int>(x => x == 1 ? dataType : null);

            var editor = new PublishValuesMultipleValueEditor(false, Mock.Of<ILogger>(), new ValueEditorAttribute("alias", "name", "view"));

            var prop = new Property(1, new PropertyType(new DataType(1, "Test.TestEditor")));
            prop.SetValue("1234,4567,8910");

            var result = editor.ConvertDbToString(prop.PropertyType, prop.GetValue(), dataTypeService);

            Assert.AreEqual("Value 1,Value 2,Value 3", result);
        }

        [Test]
        public void DropDownValueEditor_Format_Data_For_Cache()
        {
            var dataType = new DataType("editorAlias")
            {
                Configuration = new ValueListConfiguration
                {
                    Items = new List<ValueListConfiguration.ValueListItem>
                    {
                        new ValueListConfiguration.ValueListItem { Id = 10, Value = "Value 1" },
                        new ValueListConfiguration.ValueListItem { Id = 1234, Value = "Value 2" },
                        new ValueListConfiguration.ValueListItem { Id = 11, Value = "Value 3" }
                    }
                }
            };

            var dataTypeService = Mock.Of<IDataTypeService>();

            Mock.Get(dataTypeService)
                .Setup(x => x.GetDataType(It.IsAny<int>()))
                .Returns<int>(x => x == 1 ? dataType : null);

            var editor = new PublishValueValueEditor(new ValueEditorAttribute("alias", "name", "view"), Mock.Of<ILogger>());

            var prop = new Property(1, new PropertyType(new DataType(1, "Test.TestEditor")));
            prop.SetValue("1234");

            var result = editor.ConvertDbToString(prop.PropertyType, prop.GetValue(), dataTypeService);

            Assert.AreEqual("Value 2", result);
        }

        [Test]
        public void DropDownPreValueEditor_Format_Data_For_Editor()
        {
            // editor wants ApplicationContext.Current.Services.TextService
            // (that should be fixed with proper injection)
            var logger = Mock.Of<ILogger>();
            var textService = new Mock<ILocalizedTextService>();
            textService.Setup(x => x.Localize(It.IsAny<string>(), It.IsAny<CultureInfo>(), It.IsAny<IDictionary<string, string>>())).Returns("blah");
            //var appContext = new ApplicationContext(
            //    new DatabaseContext(TestObjects.GetIDatabaseFactoryMock(), logger, Mock.Of<IRuntimeState>(), Mock.Of<IMigrationEntryService>()),
            //    new ServiceContext(
            //        localizedTextService: textService.Object
            //    ),
            //    Mock.Of<CacheHelper>(),
            //    new ProfilingLogger(logger, Mock.Of<IProfiler>()))
            //{
            //    //IsReady = true
            //};
            //Current.ApplicationContext = appContext;

            var defaultVals = new Dictionary<string, object>();
            var configuration = new ValueListConfiguration
            {
                Items = new List<ValueListConfiguration.ValueListItem>
                {
                    new ValueListConfiguration.ValueListItem { Id = 1, Value = "Item 1" },
                    new ValueListConfiguration.ValueListItem { Id = 2, Value = "Item 2" },
                    new ValueListConfiguration.ValueListItem { Id = 3, Value = "Item 3" }
                }
            };

            var editor = new ValueListConfigurationEditor(Mock.Of<ILocalizedTextService>());

            var result = editor.ToEditor(defaultVals, configuration);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("items"));
            var items = result["items"] as IDictionary<int, IDictionary<string, object>>;
            Assert.IsNotNull(items);
            Assert.AreEqual("Item 1", items[1]["value"]);
            Assert.AreEqual("Item 2", items[2]["value"]);
            Assert.AreEqual("Item 3", items[3]["value"]);
        }

    }
}
