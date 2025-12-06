# StreamerbotScriptCompiler
A template project for developing Streamer.Bot scripts in Visual Studio Code with better syntax highlighting and project structuring.

Originally created to streamline my workflow - I couldn't bear working with single-file C# scripts anymore and wanted a better development experience with proper project organization, multiple files, and IDE features.

## Features

- **Project Structure**: Organise your code into multiple files and folders, saving you the headache of navigating a single file.
- **Build System**: Use the build Script to automatically compile into a streamerbot friendly format.
- **Quick Setup**: Get started with a few simple commands

## Setup

1. **Fork and clone the repository**
   - Click the "Fork" button at the top of this repository to create your own copy
   - Clone your fork (not the original repository):
   ```bash
   git clone https://github.com/YOUR_USERNAME/StreamerbotScriptCompiler
   cd StreamerbotScriptCompiler
   ```

2. **Initialize the project**
   - Open the folder in Visual Studio Code, then open a terminal and run:
   ```powershell
   .\init.ps1
   ```
   - When prompted, enter your Streamer.Bot root directory (e.g., `D:\Documents\Streamerbot`)

3. **Create a new project**
   ```powershell
   .\new.ps1 "MyProject"
   ```
   - This creates a folder in `Projects\MyProject` with a `Main.cs` file as your entry point.
   - You can have as many projects as you'd like in your local repo, as long as you do namespacing correctly to avoid conflicts.
   
   *Note: new projects will always be created in a sub-directory called `Projects` and can only be built from that folder.*

4. **Build and deploy**
   - When ready to use/test in Streamer.Bot:
   ```powershell
   .\build.ps1 MyProject
   ```
      
   This generates `Builds\<ProjectPath>\CPHInline.sbot`. Copy the contents and paste into a C# Execute Code action in Streamer.Bot, then click "Find References" before compiling.

## Usage Example
```powershell
# Create a new Project
.\new.ps1 Bingo                    # Create a Standalone Project Folder
.\new.ps1 Fun\Bingo                # Create a nested directory

# Build for use in StreamerBot
.\build.ps1 Bingo                  # Project Name
.\build.ps1 Fun\Bingo              # Partial Path
.\build.ps1 .\Projects\Fun\Bingo   # Full Path

# Build with custom output name
.\build.ps1 Bingo "Bingo-v1_0_2.cs"
   
```

### Notes:
Buidling via Project Name will build the first instance of the name it comes across, so you will need to specify the full/partial path if multiple projects share the same name.

## Project Structure

```
StreamerbotScriptCompiler/
├── Projects/
│   ├── MyProject/
│   │   ├── Main.cs              # Entry point
│   │   ├── Helpers/
│   │       └── StringHelper.cs
│   │   └── Models/
│   └── Commissions/
│       └── Example/
│           └── MyProject/
├── Builds/                      # Generated output files
├── Template.cs                  # Project template
├── build.ps1                    # Build script
├── new.ps1                      # Project creation 
└── init.ps1                     # Setup script
```

## Contributing vs. Using This Template
- **To use this template for your projects**: Fork the repository and work in your fork
- **To contribute improvements to the template**: Fork, make changes, then submit a pull request to the original repository
- **Your personal projects should stay in your fork** - don't submit PRs with your specific Streamer.Bot scripts

## Credits

Template project based on [Rondhi's gist](https://gist.github.com/rondhi/aa5e8c3b7d1277d1c93dd7f486b596fe), enhanced with automation and better project structure support.

## Disclaimer

PowerShell isn't my strongest language, and AI assistance was used in creation of the build script. It works well, but there may be room for optimization!
