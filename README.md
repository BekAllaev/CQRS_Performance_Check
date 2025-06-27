# CQRS Performance check

CQRS is well described in this article - [here](https://www.kurrent.io/event-sourcing?__hstc=265865680.f5dd684ba2ae989c1875d6e830685cb6.1744616937314.1744616937314.1744616937314.1&__hssc=265865680.1.1744616937314&__hsfp=1810455544#CQRS).

[Here](https://www.kurrent.io/event-sourcing?__hstc=265865680.f5dd684ba2ae989c1875d6e830685cb6.1744616937314.1744616937314.1744616937314.1&__hssc=265865680.1.1744616937314&__hsfp=1810455544#Read-model) is the link to particular part of the article above, where author talk about read model.

## Description

Have you ever faced a problem that your query against your data is too slow?   
It can happens when you use `INNER JOIN` or `GROUP BY`(or maybe you use `C# LINQ`) statements a lot and plus when you have big amount of data, then your request indeed will be very slow.
I have faced the same problem and I came to idea that **CQRS** can help me. 

How actually it can help? So, I seperated my model to write and read stacks. Where read stack in my case is the storage of result of "heavy" queries.
In order to check that request against query result is really faster than executing and waiting for query to finish I have created this repo.

## How to launch?

Just download the repo and press F5, SQLite database is already created and full of data.
It contains 200 items of Category entity and about 1,100,000 items of Product

## Results

> Take into account that first several requests will take longer since it takes ASP .NET Core Web API to do some inner routines as a result it takes time, so start to write down the result after several HTTP requests

On this screenshot you can see the request against endpoint that returns you the results of the query

![image](https://github.com/user-attachments/assets/969ec286-da49-4d56-b739-725f8df2e6d3)

On this screenshot you can see the request against endpoint that executes query that uses `GROUP BY` statement in it

![image](https://github.com/user-attachments/assets/bdf872e1-74bd-4e16-8706-ac2ab7d94481)

On this screenshot you can see the request against endpoint that executes query that don't use `GROUP BY` statement in it but still calculates query on demand

![image](https://github.com/user-attachments/assets/3d5270f5-38b5-4b5d-9b5c-f348e6300ab3)

## Conclusion
I actually can see that reading query result is faster than calculating query. Actually it is faster in about 10 times. The only thing left is to keep updating information that is stored in the table where query result is but user won't feel it since in the background you will update this data granullary even if you will re-calculate data in general using "heavy" query user won't feel since in mean time he/she will do other staff but not waiting for your query to complete
