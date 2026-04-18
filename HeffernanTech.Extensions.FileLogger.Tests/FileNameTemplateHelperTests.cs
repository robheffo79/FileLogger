using System;
using Xunit;
using HeffernanTech.Extensions.FileLogging;

namespace HeffernanTech.Extensions.FileLogger.Tests
{
    public class FileNameTemplateHelperTests
    {
        [Fact]
        public void SanitizeFileNamePart_WithValidInput_ReturnsUnchanged()
        {
            var result = FileNameTemplateHelper.SanitizeFileNamePart("valid-filename");
            Assert.Equal("valid-filename", result);
        }

        [Fact]
        public void SanitizeFileNamePart_WithInvalidFileNameChars_ReplacesWithUnderscore()
        {
            var result = FileNameTemplateHelper.SanitizeFileNamePart("file<name>.txt");
            Assert.Equal("file_name_.txt", result);
        }

        [Fact]
        public void SanitizeFileNamePart_WithWhitespace_ReplacesWithUnderscore()
        {
            var result = FileNameTemplateHelper.SanitizeFileNamePart("file name");
            Assert.Equal("file_name", result);
        }

        [Fact]
        public void SanitizeFileNamePart_WithTabAndNewline_ReplacesWithUnderscore()
        {
            var result = FileNameTemplateHelper.SanitizeFileNamePart("file\tname\nhere");
            Assert.Equal("file_name_here", result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void SanitizeFileNamePart_WithNullOrWhitespace_ReturnsEmpty(String input)
        {
            var result = FileNameTemplateHelper.SanitizeFileNamePart(input);
            Assert.Equal(String.Empty, result);
        }

        [Fact]
        public void SanitizeFileNamePart_WithColonAndAsterisk_ReplacesWithUnderscore()
        {
            var result = FileNameTemplateHelper.SanitizeFileNamePart("file:name*");
            Assert.Equal("file_name_", result);
        }

        [Fact]
        public void SanitizeFileNamePart_WithPipeAndQuestion_ReplacesWithUnderscore()
        {
            var result = FileNameTemplateHelper.SanitizeFileNamePart("file|name?");
            Assert.Equal("file_name_", result);
        }

        [Fact]
        public void CleanupFileName_WithValidInput_ReturnsUnchanged()
        {
            var result = FileNameTemplateHelper.CleanupFileName("valid-filename.log");
            Assert.Equal("valid-filename.log", result);
        }

        [Fact]
        public void CleanupFileName_WithDoubleUnderscore_ReducesToSingle()
        {
            var result = FileNameTemplateHelper.CleanupFileName("file__name.log");
            Assert.Equal("file_name.log", result);
        }

        [Fact]
        public void CleanupFileName_WithMultipleDoubleUnderscores_ReducesAllToSingle()
        {
            var result = FileNameTemplateHelper.CleanupFileName("file__name__here.log");
            Assert.Equal("file_name_here.log", result);
        }

        [Fact]
        public void CleanupFileName_WithUnderscoreBeforeDot_RemovesUnderscore()
        {
            var result = FileNameTemplateHelper.CleanupFileName("filename_.log");
            Assert.Equal("filename.log", result);
        }

        [Fact]
        public void CleanupFileName_WithLeadingUnderscore_Trimmed()
        {
            var result = FileNameTemplateHelper.CleanupFileName("_filename.log");
            Assert.Equal("filename.log", result);
        }

        [Fact]
        public void CleanupFileName_WithTrailingUnderscore_Trimmed()
        {
            var result = FileNameTemplateHelper.CleanupFileName("filename_.log");
            Assert.Equal("filename.log", result);
        }

        [Fact]
        public void CleanupFileName_WithInvalidChars_SanitizesAndCleans()
        {
            var result = FileNameTemplateHelper.CleanupFileName("file<name>:test?.log");
            Assert.Equal("file_name_test.log", result);
        }

        [Fact]
        public void CleanupFileName_WithOnlyUnderscores_ReturnsDefault()
        {
            var result = FileNameTemplateHelper.CleanupFileName("___");
            Assert.Equal("log.log", result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void CleanupFileName_WithNullOrWhitespace_ReturnsDefault(String input)
        {
            var result = FileNameTemplateHelper.CleanupFileName(input);
            Assert.Equal("log.log", result);
        }

        [Fact]
        public void CleanupFileName_WithInvalidCharsOnly_ReturnsDefault()
        {
            var result = FileNameTemplateHelper.CleanupFileName("<<<>>>");
            Assert.Equal("log.log", result);
        }

        [Fact]
        public void CleanupFileName_ComplexScenario_HandlesAllCases()
        {
            var result = FileNameTemplateHelper.CleanupFileName("_file<name>__test:_.log");
            Assert.Equal("file_name_test.log", result);
        }
    }
}
