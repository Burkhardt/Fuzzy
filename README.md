# Fuzzy
FuzzyRules, Linguistic variables and FuzzyTerms written originally in .NET Core 2.x, now updated to net8.0.

This repository hosts a solution named `Fuzzy`, which comprises three main components: `fuzzify` (a console application), `FuzzyLib` (a class library), and `Fuzzy.Tests` (an xUnit test project). The following sections provide guidance on building, running, and testing the solution.

## Prerequisites

Before you begin, ensure you have the following installed:
- .NET 8.0 SDK or later
- Git

## Cloning the Repository

To clone the repository and explore the solution, execute the following command:

```bash
git clone https://github.com/Burkhardt/Fuzzy.git
cd Fuzzy
```

## Building the Solution

Build the entire solution by running the following command in the root directory of the cloned repository:

```bash
dotnet build
```

This command compiles the solution, ensuring all projects are ready to run or test.

## Running the `fuzzify` Console Application

To run the `fuzzify` console application, navigate to its project directory and use the `dotnet run` command as follows:

```bash
cd fuzzify
dotnet run -- <Arguments>
```

Replace `<Arguments>` with the command-line parameters required by your application. For example:

```bash
dotnet run -- -h
-h, --help		shows this help text

-kpi n x			shows the resulting terms if linguistic variable n is set to x
    					n: 0..4
    					x: any rational number, i.e. 3.14

-l, --list			lists the lingustic variables with their names and terms
```

This example assumes your application includes a help option. Adjust the command based on the actual arguments your application supports.

## Running Tests

To execute the unit tests in the `Fuzzy.Tests` project, navigate to its directory and run:

```bash
cd Fuzzy.Tests
dotnet test
```

This will run all tests within the project, reporting the outcomes in your terminal.

## Contributing

Contributions are welcome! Feel free to submit pull requests, report issues, or suggest enhancements to this project. Please ensure you follow the project's contribution guidelines.

## License

This project is licensed under the MIT License. For more details, see the [LICENSE](https://github.com/Burkhardt/Fuzzy/blob/main/LICENSE) file in the repository.

---

This README provides a concise guide for interacting with your project hosted on GitHub, including how to build, run, and test the solution, along with how to contribute and a reference to the project's license. Adjust any project-specific details as necessary to ensure accuracy.