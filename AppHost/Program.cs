var gptDeployment = "gpt-35-turbo";
var adaDeployment = "text-embedding-ada-002";

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var openai = // an Azure OpenAI instance with a few deployments
             builder.AddAzureOpenAI("openai")
                    .AddDeployment(new(gptDeployment, gptDeployment, "0613"))
                    .AddDeployment(new(adaDeployment, adaDeployment, "2"));

var qdrant = // the qdrant container the app will use for vector search
    builder.AddContainer("qdrant", "qdrant/qdrant")
           .WithEndpoint(port: 6333, name: "qdrant", scheme: "http");

var pubsub = // a redis container the app will use for simple messaging to the frontend
    builder.AddRedis("pubsub");

var storage = // an azure storage account
    builder.AddAzureStorage("storage")
           .RunAsEmulator(); // use azurite for local development

var blobs =   // a blob container in the storage account
    storage.AddBlobs("AzureBlobs");

var queues =  // a queue in the storage account
    storage.AddQueues("AzureQueues");

var backend = // the main .net app that will perform augmentation and vector search
    builder.AddProject<Projects.Backend>("backend")
           .WithEnvironment("QDRANT_ENDPOINT", qdrant.GetEndpoint("qdrant"))
           .WithEnvironment("AZURE_OPENAI_GPT_NAME", gptDeployment)
           .WithEnvironment("AZURE_OPENAI_TEXT_EMBEDDING_NAME", adaDeployment)
           .WithEnvironment("AZURE_OPENAI_ENDPOINT", openai.Resource.ConnectionString)
           .WithReference(pubsub)
           .WithReference(blobs)
           .WithReference(queues)
           .WithReference(openai);

_ =           // a blazor server app that will provide a web ui for the app
    builder.AddProject<Projects.Frontend>("frontend")
           .WithReference(backend)
           .WithReference(pubsub)
           .WithReference(queues)
           .WithReference(openai);

builder.Build().Run();





