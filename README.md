# CQRS Performance check

CQRS is well described in this article - [here](https://www.kurrent.io/event-sourcing?__hstc=265865680.f5dd684ba2ae989c1875d6e830685cb6.1744616937314.1744616937314.1744616937314.1&__hssc=265865680.1.1744616937314&__hsfp=1810455544#CQRS).

[Here](https://www.kurrent.io/event-sourcing?__hstc=265865680.f5dd684ba2ae989c1875d6e830685cb6.1744616937314.1744616937314.1744616937314.1&__hssc=265865680.1.1744616937314&__hsfp=1810455544#Read-model) is the link to particular part of the article above, where author talk about read model.

## Description

Have you ever faced a problem that your query against your data is too slow?   
It can happen when you use `INNER JOIN` or `GROUP BY`(or maybe you use `C# LINQ`) statements a lot and plus when you have big amount of data, then your request indeed will be very slow.
I have faced the same problem and I came to idea that **CQRS** can help me. 

How actually it can help? So, I seperated my model to write and read stacks. Where read stack in my case is the storage of result of "heavy" queries.
In order to check that request against query result is really faster than executing and waiting for query to finish I have created this repo.

## How to launch?

Just download the repo and press F5, SQLite database is already created and full of data.
It contains 200 items of Category entity and about 1,100,000 items of Product

## Results

