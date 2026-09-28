# masstransit-lab

Dois processos separados, o `Publisher` e o `Consumer`, publicam e consomem mensagens pelo RabbitMQ. Cada um sobe no próprio host. Os dois não se referenciam; só conhecem o broker.

O `Publisher` só publica. Ele não declara fila. O `Consumer` declara uma fila por evento e a ligação com o exchange da mensagem.

## OrderSubmitted, definido nos dois serviços

`Publisher` e `Consumer` não compartilham o assembly desse evento. Cada um declara o próprio `OrderSubmitted`. O MassTransit identifica a mensagem pelo namespace e pelo nome do tipo (`Contracts.OrderSubmitted`), então as duas declarações precisam coincidir:

- o mesmo namespace
- o mesmo nome do tipo
- as mesmas propriedades, com os mesmos nomes

## OrderShipped, projeto compartilhado

O projeto `Contracts` declara `OrderShipped`. `Publisher` e `Consumer` referenciam esse projeto, então os dois usam o mesmo tipo em vez de copiar o contrato. O consumer recebe o evento na fila `order-shipped`.

## OrderCancelled, namespaces diferentes

`Publisher/Contracts/OrderCancelled.cs` declara o tipo em `namespace Publisher.Contracts`. `Consumer/Contracts/OrderCancelled.cs` declara outro, em `namespace Consumer.Contracts`. As propriedades têm os mesmos nomes. Os dois arquivos repetem a mesma identidade explícita:

```csharp
[EntityName("Contracts:OrderCancelled")]
[MessageUrn("Contracts:OrderCancelled")]
public record OrderCancelled
```

`EntityName` faz os dois lados usarem o exchange `Contracts:OrderCancelled`. `MessageUrn` gera o identificador `urn:message:Contracts:OrderCancelled` no envelope e no `Consume`. O namespace de cada serviço deixa de entrar na rota porque o texto dos atributos é o mesmo nos dois contratos.

## Como a configuração do MassTransit liga os dois

Os dois processos falam com o mesmo broker. A seção `RabbitMq` do `appsettings.json` (no host) ou as variáveis `RabbitMq__*` (no Compose) alimentam `cfg.Host` dentro de `AddMassTransit`. Host, virtual host, usuário e senha precisam apontar para o mesmo RabbitMQ. O publisher não precisa saber o endereço do consumer.

### Publisher

`Publisher/Program.cs` registra o barramento e escolhe o transporte RabbitMQ. Não há `AddConsumer` nem `ConfigureEndpoints`, então esse processo não cria fila.

```csharp
builder.Services.AddMassTransit(bus =>
{
    bus.UsingRabbitMq((_, cfg) =>
    {
        cfg.Host(rabbitMq.Host, rabbitMq.VirtualHost, host =>
        {
            host.Username(rabbitMq.Username);
            host.Password(rabbitMq.Password);
        });
    });
});
```

`Publisher/Api.cs` recebe `IPublishEndpoint` nos endpoints HTTP. `Publish` envia a mensagem para um exchange cujo nome o MassTransit deriva do tipo: o namespace mais o nome da classe. `OrderSubmitted` vai para o exchange de `Contracts.OrderSubmitted`. `OrderShipped` vai para o exchange de `Contracts.OrderShipped`. `OrderCancelled` usa o nome do atributo `EntityName`, `Contracts:OrderCancelled`. Quem publica não escolhe a fila.

### Consumer

`Consumer/Program.cs` registra um consumer por evento, define o formato do nome da fila e manda o MassTransit criar os endpoints a partir desses registros.

```csharp
builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<OrderSubmittedConsumer>();
    bus.AddConsumer<OrderShippedConsumer>();
    bus.AddConsumer<OrderCancelledConsumer>();
    bus.SetKebabCaseEndpointNameFormatter();

    bus.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMq.Host, rabbitMq.VirtualHost, host =>
        {
            host.Username(rabbitMq.Username);
            host.Password(rabbitMq.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

`AddConsumer` associa a classe ao tipo da mensagem. `SetKebabCaseEndpointNameFormatter` transforma o nome da classe em fila: `order-submitted`, `order-shipped` e `order-cancelled`. `ConfigureEndpoints` declara cada fila no RabbitMQ e cria o binding com o exchange do tipo consumido.

Quando uma mensagem chega, o MassTransit desserializa o corpo para o tipo do consumer e chama `Consume`. O `GET /orders`, o `GET /shipments` e o `GET /cancellations` só leem o que esses métodos já gravaram em memória.

### O que precisa coincidir

A fila só recebe a mensagem quando o tipo publicado e o tipo consumido geram o mesmo exchange.

`OrderSubmitted` está copiado em `Publisher/Contracts/OrderSubmitted.cs` e `Consumer/Contracts/OrderSubmitted.cs`. Os arquivos são independentes. Os dois usam `namespace Contracts` e o nome `OrderSubmitted`, com as mesmas propriedades. O assembly de cada serviço fica de fora dessa identidade.

`OrderShipped` está só em `Contracts/OrderShipped.cs`. A referência de projeto faz os dois serviços compilarem contra o mesmo tipo, então namespace, nome e propriedades já são os mesmos.

Se o namespace ou o nome do tipo divergir e não houver `EntityName` e `MessageUrn` iguais nos dois lados, o publisher grava em um exchange e o consumer escuta outro. A fila fica vazia e o `Consume` não roda. `OrderCancelled` é o exemplo em que os namespaces divergem de propósito e os atributos fixam a identidade em `Contracts:OrderCancelled`. Por isso o consumer sobe antes do publisher: ele precisa criar a fila e o binding antes da primeira publicação. Sem fila ligada ao exchange, o RabbitMQ descarta a mensagem.

## Pré-requisito

[Docker](https://docs.docker.com/get-docker/) com o Compose disponível no terminal.

## Testar com Docker Compose

Na raiz do repositório:

```bash
docker compose up -d --build
```

O Compose sobe os três serviços nesta ordem:

1. `rabbitmq` fica saudável.
2. `consumer` sobe, cria as filas `order-submitted`, `order-shipped` e `order-cancelled`, e só então o healthcheck dele passa.
3. `publisher` sobe depois do consumer.

Dentro da rede do Compose, `RabbitMq__Host` dos dois projetos aponta para o serviço `rabbitmq`. No host, as portas publicadas são:

| Serviço    | URL                          | Credencial |
|------------|------------------------------|------------|
| Publisher  | http://localhost:5015        |            |
| Consumer   | http://localhost:5193        |            |
| RabbitMQ   | http://localhost:15672       | `lab` / `lab` |
| AMQP       | `localhost:5672`             | `lab` / `lab` |

Confira se os containers estão saudáveis:

```bash
docker compose ps
```

Publique um pedido:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5015/orders -ContentType "application/json" -Body '{"customer":"Ada Lovelace","amount":42.5}'
```

O consumer grava o pedido recebido. Consulte a lista:

```powershell
Invoke-RestMethod http://localhost:5193/orders
```

A resposta do `POST` e o item retornado pelo `GET` trazem o mesmo `orderId`.

Publique o envio do mesmo pedido. Esse evento vem do projeto `Contracts`:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5015/orders/11111111-1111-1111-1111-111111111111/ship -ContentType "application/json" -Body '{"trackingCode":"BR123456789"}'
Invoke-RestMethod http://localhost:5193/shipments
```

Publique o cancelamento. Os dois contratos estão em namespaces diferentes e compartilham o exchange `Contracts:OrderCancelled`:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5015/orders/11111111-1111-1111-1111-111111111111/cancel -ContentType "application/json" -Body '{"reason":"cliente desistiu"}'
Invoke-RestMethod http://localhost:5193/cancellations
```

No painel do RabbitMQ (`http://localhost:15672`, usuário `lab`, senha `lab`), as filas `order-submitted`, `order-shipped` e `order-cancelled` aparecem em Queues. O log do consumer mostra o pedido recebido, o rastreio e o cancelamento.

Para acompanhar o consumo:

```bash
docker compose logs -f consumer
```

Para encerrar e remover os containers:

```bash
docker compose down
```

## Testar no host

Use este caminho quando quiser depurar os projetos fora do container. Suba só o broker:

```bash
docker compose up -d rabbitmq
```

O `appsettings.json` de cada projeto já aponta para `localhost` com usuário `lab` e senha `lab`. Suba o consumer primeiro, para a fila existir antes da primeira publicação. Em dois terminais:

```bash
dotnet run --project Consumer
dotnet run --project Publisher
```

O publisher escuta em `http://localhost:5015` e o consumer em `http://localhost:5193`. Use os mesmos comandos `Invoke-RestMethod` da seção anterior.

Se o Compose inteiro já estiver no ar, pare os containers `publisher` e `consumer` antes do `dotnet run`. As portas `5015` e `5193` são as mesmas.

## Apontar para outro broker

No Compose, altere as variáveis `RabbitMq__Host`, `RabbitMq__VirtualHost`, `RabbitMq__Username` e `RabbitMq__Password` do serviço correspondente.

No host, altere a seção `RabbitMq` do `appsettings.json` daquele projeto.
