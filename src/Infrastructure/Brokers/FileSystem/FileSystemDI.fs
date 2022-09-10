namespace Infrastructure.DI.Brokers

module FileSystemDI =

    type IPathBroker = Brokers.FileSystem.Path.Broker
    type IDataBroker = Brokers.FileSystem.Data.Broker
