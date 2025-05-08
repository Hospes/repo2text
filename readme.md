# Repo2Text - Standalone Windows App

A native Windows desktop application built with C# and Windows Forms to convert GitHub repositories, local directories, or local ZIP files into a single, formatted plain text file. This is useful for feeding code into Large Language Models (LLMs), documentation, or analysis.

This project is a port of the original web-based Repo2Text tool.

## Features

*   **Fetch from GitHub:**
    *   Input a GitHub repository URL (including specific branches or sub-paths).
    *   Supports public repositories.
    *   Supports private repositories using a Personal Access Token (PAT).
    *   Token is stored locally in user settings for convenience (use with caution).
*   **Process Local Sources:**
    *   Select a local directory to process its contents.
    *   Upload and process local ZIP archives.
    *   Handles `.gitignore` files found in local directories or ZIP archives to exclude specified files/folders.
*   **Interactive File Selection:**
    *   Displays the directory structure in a TreeView.
    *   Allows selective checking/unchecking of files and folders.
*   **Output Generation:**
    *   Generates a single plain text file containing:
        *   A representation of the selected directory structure.
        *   The content of all selected files, clearly marked with their paths.
*   **Convenience Features:**
    *   **Copy to Clipboard:** Easily copy the generated text.
    *   **Download Text:** Save the generated text as a `.txt` file.
    *   **Download Selected as ZIP (GitHub Only):** Download the *selected* files directly from GitHub as a new ZIP archive.

## How to Use

1.  **(Optional) Download:** Grab the latest release from the [Releases](link/to/your/releases/page) page. Unzip and run `Repo2Text.exe`.
2.  **Choose Source:**
    *   **GitHub:** Enter the full GitHub repository URL (e.g., `https://github.com/owner/repo` or `https://github.com/owner/repo/tree/branch/path`). Optionally, enter a Personal Access Token for private repos or higher rate limits. Click `Fetch from GitHub`.
    *   **Local Directory:** Click `Select Local Directory` and choose the root folder of the project you want to process.
    *   **Local ZIP:** Click `Select Local ZIP` and choose the ZIP archive you want to process.
3.  **Select Files:** The directory structure will appear. Use the checkboxes to select the files and folders you want to include in the output. Uncheck items you wish to exclude.
4.  **Generate Output:**
    *   Click `Generate Text` to create the combined text file in the output text box.
    *   *(GitHub Source Only)* Click `Download Selected as ZIP` to download only the selected files from the GitHub repository into a new ZIP file.
5.  **Use Output:**
    *   Click `Copy to Clipboard` to copy the generated text.
    *   Click `Download Text` to save the generated text to a `.txt` file.

## Technology Stack

*   **Language:** C#
*   **Framework:** .NET [Specify Version, e.g., 6.0]
*   **UI:** Windows Forms (WinForms)
*   **GitHub API:** [Octokit.net](https://github.com/octokit/octokit.net) library
*   **ZIP Handling:** System.IO.Compression (.NET built-in)

## Getting Started (Development)

### Prerequisites

*   .NET SDK [Specify Version, e.g., 6.0 or later]
*   Visual Studio 2022 (Recommended)

### Building and Running

1.  **Clone the repository:**
    ```bash
    git clone [Your Repository URL Here]
    cd Repo2Text-Windows 
    ```
    <!-- *Replace [Your Repository URL Here] with the actual URL.* -->
2.  **Open the solution:** Open the `.sln` file in Visual Studio.
3.  **Restore NuGet Packages:** Visual Studio should do this automatically upon opening the solution. If not, right-click the Solution in Solution Explorer and choose "Restore NuGet Packages". (Requires Octokit.net).
4.  **Build the solution:** Press `F6` or go to `Build > Build Solution`.
5.  **Run the application:** Press `F5` or click the Start button (usually a green triangle) in the toolbar.

## Configuration

*   **GitHub Access Token:** The application allows storing an optional GitHub Personal Access Token (PAT) to access private repositories or avoid rate limits. This token is stored in the user's local application settings (`Properties.Settings`). While convenient, be aware of the security implications of storing tokens locally. For highly sensitive scenarios, consider more secure methods not implemented here (like Windows Credential Manager).

## Contributing

Contributions are welcome! If you find a bug or have a feature request, please open an issue on GitHub. If you'd like to contribute code, please fork the repository and submit a pull request.

1.  Fork the Project
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3.  Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4.  Push to the Branch (`git push origin feature/AmazingFeature`)
5.  Open a Pull Request

## License

Distributed under the MIT License. See `LICENSE` file for more information.

## Acknowledgements

*   Based on the original web-based [Repo2Text](https://github.com/abinthomasonline/repo2txt) project by [abinthomasonline](https://github.com/abinthomasonline).
*   Uses the excellent [Octokit.net](https://github.com/octokit/octokit.net) library.