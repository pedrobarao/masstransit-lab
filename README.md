# masstransit-lab

Dois processos separados, o `Publisher` e o `Consumer`, publicam e consomem a mesma mensagem pelo RabbitMQ. Cada um sobe no próprio host e não referencia o projeto do outro.

O `Publisher` só publica. Ele não declara fila. O `Consumer` declara a fila `order-submitted` e a ligação com o exchange da mensagem. Os dois só precisam conhecer o broker, não o endereço um do outro.

## Contrato definido nos dois serviços

`Publisher` e `Consumer` não referenciam o mesmo assembly. Cada um declara o próprio `OrderSubmitted`. O MassTransit identifica a mensagem pelo namespace e pelo nome do tipo (`MassTransitLab.Contracts.OrderSubmitted`), então as duas declarações precisam coincidir:

- o mesmo namespace
- o mesmo nome do tipo
- as mesmas propriedades, com os mesmos nomes

O projeto `Contracts` guarda uma cópia dessa forma para consulta. Nenhum dos dois serviços o referencia.

## Pré-requisito

[Docker](https://docs.docker.com/get-docker/) com o Compose disponível no terminal.

## Testar com Docker Compose

Na raiz do repositório:

```bash
docker compose up -d --build
```

O Compose sobe os três serviços nesta ordem:

1. `rabbitmq` fica saudável.
2. `consumer` sobe, cria a fila `order-submitted` e só então o healthcheck dele passa.
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

No painel do RabbitMQ (`http://localhost:15672`, usuário `lab`, senha `lab`), a fila `order-submitted` aparece em Queues. O exchange da mensagem aparece em Exchanges com o nome derivado de `MassTransitLab.Contracts.OrderSubmitted`.

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
