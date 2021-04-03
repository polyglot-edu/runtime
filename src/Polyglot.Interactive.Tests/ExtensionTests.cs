﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Polyglot.Interactive.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Polyglot.Interactive.Tests
{
    [Collection("no parallel")]
    public class ExtensionTests : LanguageKernelTestBase
    {
        public ExtensionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task kernel_extension_loads()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();

            await extension.OnLoadAsync(kernel);

            KernelEvents.Should().NotContainErrors();

        }

        [Fact]
        public async Task adds_command_to_configure_game_engine()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();
            await extension.OnLoadAsync(kernel);
            
            kernel.Directives.Should().ContainSingle(d => d.Name == "#!start-game");
        }

        [Fact]
        public async Task user_can_configure_game_engine()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();
            await extension.OnLoadAsync(kernel);


            GameEngineClient.Current.Should().BeNull();
            var result = await kernel.SendAsync(new SubmitCode("#!start-game --player-id playerOne --user-id papyrus --game-id 603fced708813b0001baa2cc --password papyrus0704!"), CancellationToken.None);
            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            GameEngineClient.Current.Should().NotBeNull();
        }

        [Fact]
        public async Task adds_middleware_to_intercept_code_submissions()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();
            await extension.OnLoadAsync(kernel);
            
            await kernel.SendAsync(new SubmitCode("#!start-game --player-id playerOne --user-id papyrus --game-id 603fced708813b0001baa2cc --password papyrus0704!"), CancellationToken.None);

            var result = await kernel.SendAsync(new SubmitCode("\"Hello World\""), CancellationToken.None);

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<DisplayedValueProduced>(d => d.Value is GameStateReport);
        }

        [Fact]
        public async Task intercepts_variables()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();
            await extension.OnLoadAsync(kernel);

            await kernel.SendAsync(new SubmitCode("#!start-game --player-id playerOne --user-id papyrus --game-id 603fced708813b0001baa2cc --password papyrus0704!"), CancellationToken.None);

            var result = await kernel.SendAsync(new SubmitCode("var newVariable = \"Hello World\";"), CancellationToken.None);

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<DisplayedValueProduced>(d => d.Value is GameStateReport);
        }

        [Fact]
        public async Task does_not_intercept_directives()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();
            await extension.OnLoadAsync(kernel);

            await kernel.SendAsync(new SubmitCode("#!start-game --player-id playerOne --user-id papyrus --game-id 603fced708813b0001baa2cc --password papyrus0704!"), CancellationToken.None);

            var result = await kernel.SendAsync(new SubmitCode("#!lsmagic"), CancellationToken.None);

            result.KernelEvents
                .ToSubscribedList()
                .OfType<DisplayedValueProduced>()
                .Should()
                .NotContain(d => d.Value is GameStateReport);
        }

        [Fact]
        public async Task intercepts_errors()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();
            await extension.OnLoadAsync(kernel);

            await kernel.SendAsync(new SubmitCode("#!start-game --player-id playerOne --user-id papyrus --game-id 603fced708813b0001baa2cc --password papyrus0704!"), CancellationToken.None);

            await kernel.SendAsync(new SubmitCode("not valid at all"), CancellationToken.None);

            var report = await GameEngineClient.Current.GetReportAsync();

            report.Points.Should().Be(0);
        }

        [Fact]
        public async Task no_warnings_to_level_one()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();
            await extension.OnLoadAsync(kernel);

            await kernel.SendAsync(new SubmitCode("#!start-game --player-id playerOne --user-id papyrus --game-id 603fced708813b0001baa2cc --password papyrus0704!"), CancellationToken.None);

            await kernel.SendAsync(new SubmitCode(@"
var a = 12;
a"), CancellationToken.None);

            var report = await GameEngineClient.Current.GetReportAsync();

            report.Points.Should().Be(0);
        }

        [Fact]
        public async Task use_warnings()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();
            await extension.OnLoadAsync(kernel);

            await kernel.SendAsync(new SubmitCode("#!start-game --player-id playerOne --user-id papyrus --game-id 603fced708813b0001baa2cc --password papyrus0704!"), CancellationToken.None);

            await kernel.SendAsync(new SubmitCode(@"
var a = 12;
var b = 123;

class MyCode {
    public static int DoIt(){
        var r = 111111;
        var rr = 111111;
        var rrr = 111111;
        var rrrr = 111111;
        var rrrrr = 111111;
        Task.Run(() => {
            a = 12;
        });
        return 12;
    }
}

MyCode.DoIt();"), CancellationToken.None);

            var report = await GameEngineClient.Current.GetReportAsync();

            report.Points.Should().Be(0);
        }

        [Fact]
        public async Task runs_hardcoded_exercise()
        {
            var extension = new KernelExtension();
            var kernel = CreateKernel();
            await extension.OnLoadAsync(kernel);

            await kernel.SendAsync(new SubmitCode("#!start-game --player-id playerOne --user-id papyrus --game-id 603fced708813b0001baa2cc --password papyrus0704!"), CancellationToken.None);

            var report = await GameEngineClient.Current.GetReportAsync();

            report.CurrentLevel.Should().Be("0");
            report.Points.Should().Be(0);

            await kernel.SendAsync(new SubmitCode(@"
public class Triangle {}                
"), CancellationToken.None);

            report = await GameEngineClient.Current.GetReportAsync();

            report.CurrentLevel.Should().Be("1");
            report.Points.Should().Be(10);

            await kernel.SendAsync(new SubmitCode(@"
public class Triangle
{
    private float _base;
    private float _height;
}"), CancellationToken.None);

            report = await GameEngineClient.Current.GetReportAsync();

            report.CurrentLevel.Should().Be("2");
            report.Points.Should().Be(20);

            await kernel.SendAsync(new SubmitCode(@"
public class Triangle
{
    private float _base;
    private float _height;

    public Triangle(float base, float height)
    {
        _base = base;
        _height = height;
    }
}"), CancellationToken.None);

            report = await GameEngineClient.Current.GetReportAsync();

            report.CurrentLevel.Should().Be("3");
            report.Points.Should().Be(30);

            await kernel.SendAsync(new SubmitCode(@"
public class Triangle
{
    private float _height;
    private float _base;

    public Triangle(float base, float height)
    {
        _base = base;
        _height = height;
    }

    public float calculateArea() 
    {
        return _base*_height/2;
    }
}"), CancellationToken.None);

            report = await GameEngineClient.Current.GetReportAsync();

            report.CurrentLevel.Should().Be("4");
            report.Points.Should().Be(40);
        }
    }
}
