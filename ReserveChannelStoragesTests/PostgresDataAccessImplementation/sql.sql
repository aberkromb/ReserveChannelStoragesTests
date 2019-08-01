CREATE EXTENSION pgq;
SELECT pgq.create_queue('reserve_channel');
SELECT pgq.register_consumer('reserve_channel', 'consumer');

create table reserve_channel_messages
(
    id integer not null,
    server varchar(255) not null,
    application varchar(255) not null,
    exchange varchar(255) not null,
    message_date timestamptz not null,
    message_type varchar(255) not null,
    message_routing_key varchar(255) not null,
    message text not null,
    exception text,
    ttl int,
    persistent boolean not null,
    additional_headers varchar(255)
);

Create index on reserve_channel_messages (id)