# SSDP Device Publisher

This C# program is a Simple Service Discovery Protocol (SSDP) device publisher. It allows you to publish a UPnP root device on a specified network interface, with dynamic updates based on IP address changes.

## Getting Started

### Prerequisites

- .NET Core SDK installed
- Network interface specified in the appsettings.json file
- (Add any additional prerequisites or configuration instructions)

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/Sparse-Technology/DiscoveryApp.git
   ```

2. Navigate to the project directory:

   ```bash
   cd DiscoveryApp
   ```

3. Build the project:

   ```bash
   dotnet build
   ```

4. Run the program:

   ```bash
   dotnet run -- -i [NetworkInterfaceName] [AdditionalOptions]
   ```

### Usage

The program supports the following command-line options:

- `-i` or `--iface`: Specifies the network interface to bind to (required).
- `-n` or `--name`: Specifies the device name (optional).
- `-l` or `--location`: Specifies the device location (optional).
- `-u` or `--uuid`: Specifies the device UUID (optional).
- `-d` or `--description`: Specifies the device description (optional).
- `-m` or `--manufacturer`: Specifies the device manufacturer (optional).
- `-o` or `--model`: Specifies the device model (optional).
- `-t` or `--type`: Specifies the device type (optional).
- `-p` or `--port`: Specifies the device port (optional, default is 0).
- `-c` or `--cache-lifetime`: Specifies the device cache lifetime in minutes (optional, default is 1).

Example:

```bash
dotnet run -- -i Ethernet -n MyDevice -l /mydevice -u abc123 -d "My SSDP Device" -m "MyManufacturer" -o "MyModel" -t "MyType" -p 8080 -c 5
```

### Features

- Dynamically updates device location on IP address changes.
- Provides a simple HTTP listener for serving the device description document.

### License

This project is licensed under the [MIT License](LICENSE).

### Acknowledgments

- Thanks to the [Rssdp](https://github.com/Yortw/RSSDP) library for SSDP implementation.
