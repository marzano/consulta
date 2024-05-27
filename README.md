# Descrição 

Este serviço realiza a consulta de status do cartão personalizado SPTRANS a partir de um evento de requisição que irá realizar a navegação utilizando *Selenium Web Driver* e responder via mensageria o resultado da consulta em outra fila de forma assíncrona. 

# Como utilizar

Para solicitar uma consulta ao serviço, uma requisição via mensagem deve ser enviada para o serviço:

1. Informações para conexão:
    - **Virtual Host**: operações
    - **Exchange de requisição**: .status.cartao.requisicao.sptrans.ex

2.	A mensagem de requisição deve seguir o seguinte formato:

- **JSON**

``` json 
{
    "CorrelationId": "xd34243", //Identificador (será enviado na mensagem com o resultado)
    "CPF": "25249408885", 
    "System": "roteirizacao", //Sistema solicitante
    "ConsumerApplicationName": "svc-retorno-sptrans" //Nome da aplicação que irá consumir o resultado
}
```
 - **C#**

``` csharp 
public class RequisicaoConsultaCartaoDTO
{
    public string CorrelationId { get; set; }
    public string CPF { get; set; }
    public string System { get; set; }
    public string ConsumerApplicationName { get; set; }
}

```

3.	O Resultado da consulta será disponibilizado no exchange: **consulta.status.cartao.resultado.sptrans.ex**
4.	Para consumir somente o resultado refente a requisição específica enviada, o serviço consumidor deve criar uma fila e realizar o bind com este exchange (*tt.status.cartao.resultado.sptrans.ex*) e configurar a Routing-Key no formato *"resultado-status-cartao.**{system}**.**{consumerApplicationName}**"*
    - resultado-status-cartao - fixo 
    - {system} - Sistema solicitante enviada na requisição
    - {consumerApplicationName} - Aplicação consumidora enviada na requisição

5. Caso o consumidor necessite receber todos os eventos de atualização de status de cartão referente à um sistema específico, a routing key deve ser configurada como: *resultado-status-cartao.**{system}**.* *     

5. Para receber todos os eventos de cartão, utilize: *resultado-status-cartao.**     
