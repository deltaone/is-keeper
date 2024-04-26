# is-keeper
Программа предназначена для обслуживания коллекции архивов. Обрабатывается каталог поступления и, 
в зависимости от результата обработки, файлы распределяются по разным категориям - дубли/поврежденные/новые/обновления.

в main.ini прописать рабочие папки  
основное хранилище в ключ - storage=c:\archive  
папка поступления в ключ - income=c:\archive-income  

При запуске индексируется основное хранилище, копируем файлы для сортировки в папку поступления 
(можно скопировать заранее) - файлы обрабатываются и, в зависимости от результата, распределяются по папкам категорий 
в каталоге программы.

Правило хранения данных в каталоге хранилища - один файл = архив или файл с данными, то есть если у вас есть запакованный 
файл в хранилище и вы его копируете распакованным в поступления, он уйдет в категорию дубли.

Анализ дублей и частиных дублей (архив в хранилище содержит полную копию содержимого архива-поступления) происходит по 
контрольным суммам, то есть имена архивов и файлов значения не имеют.

Все архивы автоматически перепаковываются в формат .RAR

Поддерживаемые форматы архивов .RAR / .ZIP / .7Z
<!--
## Donation
If this project help, you can give me a cup of coffee.

| USD | RUB |
|:---:|:---:|
| [![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LN5B389QKPB2&lc=US) | [![paypal](https://www.paypalobjects.com/ru_RU/RU/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=63QTZ8AX4H3BC&source=url&lc=RU) |
-->

<p align="center"> <img src="https://komarev.com/ghpvc/?username=deltaone-is-keeper&label=Repository%20views&color=ce9927&style=flat" /> </p>
