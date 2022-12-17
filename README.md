# Polyglot

A Gamified .NET Interactive Framework for Teaching and Learning Multi-Language Programming for High School and Undergraduate Students

Polyglot works well with C# and [SysML V2](https://www.omgsysml.org/SysML-2.htm), but we're extending the support for the other .NET Interactive languages (F#, Javascript, ...)

See it in action in the videos below!




https://user-images.githubusercontent.com/41111850/139651588-cb775250-209f-4654-b811-9777544e136b.mp4




https://user-images.githubusercontent.com/41111850/139651436-ab99f0cc-312d-4cdd-b1b0-39f23cb80fe9.mp4



## How to contribute

You can clone the repo and open the solution with Visual Studio.  
The solution is composed of three projects (plus the test project):

- **Polyglot.Gamification**: Handles the communication with Polyglot backend and the integration with Journey
- **Polyglot.Interactive**: handles the integration with VSCode notebooks
- **Polyglot.Interactive.SysML**: Provides the SysML kernel abstraction for .NET Interactive
- **Polyglot.Metrics.CSharp**: has language specific metrics
- **Polyglot.Metrics.SysML**: has language specific metrics

## Requirements installation guide
### Step 1 - Install VSCode and .NET 7
Go to [https://dotnet.microsoft.com/en-us/download/dotnet/7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0), download and install the latest .NET 7 version available for your machine.  
Go to [https://code.visualstudio.com/download](https://code.visualstudio.com/download), download and install the latest VSCode version available for your machine.  
Open VSCode, go to the extensions marketplace and install the "Polyglot Notebooks" extension, version 1.0.3611020 (you can also find it at the following link [https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode))

### Step 2 - Install additional requirements for SysML
Go to [https://graphviz.org/download/](https://graphviz.org/download/), download and install GraphViz 7.* for your machine.  
During the installation process make sure to add GraphViz to PATH (at least for the current user)
Install the JRE (Java Runtime Environment) for your machine (Java 17 or above). Make sure to add the JRE to PATH (at least for the current user) so that you can run "java --version" from the command line.

### Step 3 - Use Polyglot
Open the notebook on VSCode and run:
first cell:
```
#r "nuget: Polyglot.Interactive, 0.0.2-*"
```
second cell:
```
#!polyglot-setup --flowid PUT_HERE_THE_FLOW_ID_YOU_WANT_TO_RUN --serverurl https://api.polyglot-edu.com/
```

## Contributors

- **Antonio Bucchiarone** - Fondazione Bruno Kessler (FBK), Trento - Italy

- **Diego Colombo** - Microsoft Research, Cambridge, United Kingdom

- **Tommaso Martorella** - Master's student at École Polytechnique Fédérale de Lausanne (EPFL), Switzerland - Microsoft Learn Student Ambassador

For any information, you can contact us by writing an email to bucchiarone@fbk.eu

![Visitor Count](https://profile-counter.glitch.me/{antbucc}/count.svg)
