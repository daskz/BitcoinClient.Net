# BitcoinClient.Net
Приложение .NET CORE WEB API для взаимодействия с bitcoind
## API

[GET] **api/wallets**  
Получить список кошельков с актуальным балансом

[POST] **api/wallets**  
Создать кошелек

[GET] **api/wallets/id/addresses**  
Получить список адресов по кошельку

[POST] **api/wallets/id/addresses**  
Создать адрес

[POST] **api/wallets/id/transactions (SendBtc)**  
Отправить BTC с одного из кошельков на указанный адрес

[GET] **api/wallets/id/transactions (GetLast)**  
Получить список последних поступлений на кошельки

[PUT] **api/wallets/transactions/id**  
Метод для события notifywallet

[PUT] **api/blocks/id**  
Метод для события notifyblock

## 

![Diagram](https://github.com/daskz/BitcoinClient.Net/blob/master/Diagram.png)
