﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.CodeDom.Compiler;
using System.Text;

using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.TextTemplating;
using NUnit.Framework;
using TextTemplating.Tests.Helpers;

namespace TextTemplating.Tests
{
	[TestFixture]
	public class TextTemplatingPreprocessorTests
	{
		TextTemplatingFilePreprocessor preprocessor;
		FakeTextTemplatingHost templatingHost;
		FakeTextTemplatingCustomToolContext customToolContext;
		
		[SetUp]
		public void Init()
		{
			SD.InitializeForUnitTests();
			MessageLoopHelper.InitializeForUnitTests();
		}
		
		[TearDown]
		public void TearDown()
		{
			SD.TearDownForUnitTests();
		}
		
		TestableFileProjectItem PreprocessTemplate(string fileName)
		{
			var projectFile = CreatePreprocessor(fileName);
			preprocessor.PreprocessTemplate();
			
			return projectFile;
		}
		
		TestableFileProjectItem CreatePreprocessor(string fileName)
		{
			templatingHost = new FakeTextTemplatingHost();
			var projectFile = new TestableFileProjectItem(fileName);
			customToolContext = new FakeTextTemplatingCustomToolContext();
			
			preprocessor = new TextTemplatingFilePreprocessor(templatingHost, projectFile, customToolContext);
			
			return projectFile;
		}
		
		[Test]
		public void PreprocessTemplate_TemplateFileInProjectPassed_TemplatingHostPreprocessesTemplateFile()
		{
			string fileName = @"d:\projects\MyProject\template.tt";
			PreprocessTemplate(fileName);
			
			Assert.AreEqual(fileName, templatingHost.InputFilePassedToPreprocessTemplate);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateFileInProjectPassed_EncodingPassedToTemplatingHostIsUTF8()
		{
			PreprocessTemplate(@"d:\projects\MyProject\template.tt");
			
			Assert.AreEqual(Encoding.UTF8, templatingHost.EncodingPassedToPreprocessTemplate);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateFileInProjectFilePassed_ClassNamePassedToTemplatingHostIsTemplateFileNameWithoutExtension()
		{
			PreprocessTemplate(@"d:\Test.tt");
			
			Assert.AreEqual("Test", templatingHost.ClassNamePassedToPreprocessTemplate);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateFileInCSharpProject_OutputFileNameIsTemplateFileNameWithCSharpFileExtension()
		{
			var templateFile = CreatePreprocessor(@"d:\MyProject\Test.tt");
			templateFile.TestableProject.SetLanguage("C#");
			preprocessor.PreprocessTemplate();
			
			Assert.AreEqual(@"d:\MyProject\Test.cs", templatingHost.OutputFilePassedToPreprocessTemplate);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateFileInVisualBasicProject_OutputFileNameIsTemplateFileNameWithVisualBasicFileExtension()
		{
			var templateFile = CreatePreprocessor(@"d:\MyProject\Test.tt");
			templateFile.TestableProject.SetLanguage("VBNet");
			preprocessor.PreprocessTemplate();
			
			Assert.AreEqual(@"d:\MyProject\Test.vb", templatingHost.OutputFilePassedToPreprocessTemplate);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateFileInProjectFilePassed_ClassNamespacePassedToTemplateHostIsProjectRootNamespace()
		{
			var templateFile = CreatePreprocessor(@"d:\MyProject\Test.tt");
			templateFile.Project.RootNamespace = "RootNamespace";
			preprocessor.PreprocessTemplate();
			
			Assert.AreEqual("RootNamespace", templatingHost.ClassNamespacePassedToPreprocessTemplate);
		}
		
		[Test]
		public void PreprocessTemplate_CustomToolNamespaceSet_LogicalCallContextNamespaceHintDataIsSet()
		{
			var templateFile = CreatePreprocessor(@"d:\a.tt");
			templateFile.CustomToolNamespace = "Test";
			preprocessor.PreprocessTemplate();
			
			Assert.AreEqual("NamespaceHint", customToolContext.NamePassedToSetLogicalCallContextData);
		}
		
		[Test]
		public void PreproocessTemplate_CustomToolNamespaceSet_LogicalCallContextNamespaceHintDataIsSetBefore()
		{
			var templateFile = CreatePreprocessor(@"d:\a.tt");
			templateFile.CustomToolNamespace = "Test";
			preprocessor.PreprocessTemplate();
			
			string data = customToolContext.DataPassedToSetLogicalCallContextData as String;
			
			Assert.AreEqual("Test", data);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateFileInProjectPassed_TemplateFileInProjectUsedWhenCheckingIfOutputFileExistsInProject()
		{
			var file = PreprocessTemplate(@"d:\template.tt");
			
			Assert.AreEqual(file, customToolContext.BaseItemPassedToEnsureOutputFileIsInProject);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateFileInCSharpProject_TemplateFileWithFileExtensionChangedToCSharpFileExtensionIsUsedWhenCheckingIfOutputFileExistsInProject()
		{
			var templateFile = CreatePreprocessor(@"d:\template.tt");
			templateFile.TestableProject.SetLanguage("C#");
			preprocessor.PreprocessTemplate();
			
			Assert.AreEqual(@"d:\template.cs", customToolContext.OutputFileNamePassedToEnsureOutputFileIsInProject);
		}
		
		[Test]
		public void PreprocessTemplate_MethodReturnsFalse_OutputFileNotAddedToProject()
		{
			CreatePreprocessor(@"d:\a.tt");
			templatingHost.PreprocessTemplateReturnValue = false;
			preprocessor.PreprocessTemplate();

			Assert.IsFalse(customToolContext.IsOutputFileNamePassedToEnsureOutputFileIsInProject);
		}

		[Test]
		public void PreprocessTemplate_TemplateFileInProjectPassed_TaskServiceHasClearedTasksExceptForCommentTasks()
		{
			var file = PreprocessTemplate(@"d:\template.tt");
			
			Assert.IsTrue(customToolContext.IsClearTasksExceptCommentTasksCalled);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateHostHasOneErrorAfterProcessing_ErrorTaskAddedToTaskList()
		{
			CreatePreprocessor(@"d:\a.tt");
			templatingHost.ErrorsCollection.Add(new CompilerError());
			preprocessor.PreprocessTemplate();
			
			SDTask task = customToolContext.FirstTaskAdded;
			
			Assert.AreEqual(TaskType.Error, task.TaskType);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateHostHasOneErrorAfterProcessing_ErrorDescriptionIsAddedToTaskList()
		{
			CreatePreprocessor(@"d:\a.tt");
			var error = new CompilerError();
			error.ErrorText = "error text";
			templatingHost.ErrorsCollection.Add(error);
			preprocessor.PreprocessTemplate();
			
			SDTask task = customToolContext.FirstTaskAdded;
			
			Assert.AreEqual("error text", task.Description);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateHostHasOneErrorAfterProcessing_ErrorsPadBroughtToFront()
		{
			CreatePreprocessor(@"d:\a.tt");
			templatingHost.Errors.Add(new CompilerError());
			preprocessor.PreprocessTemplate();
			
			Assert.IsTrue(customToolContext.IsBringErrorsPadToFrontCalled);
		}
		
		[Test]
		public void PreprocessTemplate_ThrowsException_ErrorTaskAddedToTaskList()
		{
			CreatePreprocessor(@"d:\a.tt");
			var ex = new Exception("error");
			templatingHost.ExceptionToThrowWhenPreprocessTemplateCalled = ex;
			preprocessor.PreprocessTemplate();
			
			SDTask task = customToolContext.FirstTaskAdded;
			
			Assert.AreEqual("error", task.Description);
		}
		
		[Test]
		public void PreprocessTemplate_ThrowsException_ErrorTaskAddedToTaskListWithTemplateFileName()
		{
			CreatePreprocessor(@"d:\a.tt");
			var ex = new Exception("invalid operation error");
			templatingHost.ExceptionToThrowWhenPreprocessTemplateCalled = ex;
			preprocessor.PreprocessTemplate();
			
			SDTask task = customToolContext.FirstTaskAdded;
			var expectedFileName = new FileName(@"d:\a.tt");
			
			Assert.AreEqual(expectedFileName, task.FileName);
		}
		
		[Test]
		public void PreprocessTemplate_ThrowsException_ExceptionLogged()
		{
			CreatePreprocessor(@"d:\a.tt");
			var ex = new Exception("invalid operation error");
			templatingHost.ExceptionToThrowWhenPreprocessTemplateCalled = ex;
			preprocessor.PreprocessTemplate();
			
			Exception exceptionLogged = customToolContext.ExceptionPassedToDebugLog;
			
			Assert.AreEqual(ex, exceptionLogged);
		}
		
		[Test]
		public void PreprocessTemplate_ThrowsException_MessageLoggedIncludesInformationAboutProcessingTemplateFailure()
		{
			CreatePreprocessor(@"d:\a.tt");
			var ex = new Exception("invalid operation error");
			templatingHost.ExceptionToThrowWhenPreprocessTemplateCalled = ex;
			preprocessor.PreprocessTemplate();
			
			string message = customToolContext.MessagePassedToDebugLog;
			string expectedMessage = "Exception thrown when processing template 'd:\\a.tt'.";
			
			Assert.AreEqual(expectedMessage, message);
		}
		
		[Test]
		public void PreprocessTemplate_TemplateFileNameIsCSharpClassKeywordInCSharpProject_ClassNameChangedToValidClassName()
		{
			PreprocessTemplate(@"d:\class.tt");
			
			Assert.AreEqual("_class", templatingHost.ClassNamePassedToPreprocessTemplate);
		}
	}
}
