# Basic Laboratory Information System (BLIS): Next Generation Desktop Launcher

The Basic Laboratory Information System (BLIS) is a LIMS that is capable of running on a desktop computer or a cloud server.

This application replaces the customized [Server2Go](https://web.archive.org/web/20120114034947/https://server2go-web.de/) launcher that BLIS has been using since its initial version with an open-source .NET application.

The binary built with this project will require the files in [BLISRuntime](https://github.com/C4G/BLISRuntime) and [BLIS](https://github.com/C4G/BLIS) to run.

## Building

To build this project, ensure you have the latest .NET Core SDK installed, and run:

```powershell
dotnet build
```

### Building a release build

To build a release build (ie. a single binary) you can run:

```powershell
dotnet publish -r win-x64 -c Release
```

## License

This project is licensed under the GNU General Public License Version 3, unless otherwise noted.
