
--------------CREATE TABLES

CREATE TABLE IF NOT EXISTS public.interaction_history
(
    sid bigint NOT NULL,
    datetime timestamp without time zone,
    action text COLLATE pg_catalog."default",
    value text COLLATE pg_catalog."default"
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.interaction_history
    OWNER to postgres;


CREATE EXTENSION postgis;



CREATE TABLE IF NOT EXISTS public.project
(
   projectname text COLLATE pg_catalog."default",
   startdate date,
   active boolean,
    projectid serial,
   notification_emailaddress text COLLATE pg_catalog."default",
   owner_userid integer
)
WITH (
    OIDS = FALSE
)
 TABLESPACE pg_default;

ALTER TABLE  project ADD PRIMARY KEY (projectid);


COMMENT ON COLUMN public.project.notification_emailaddress IS 'who to send alert emails to';
COMMENT ON COLUMN public.project.owner_userid IS 'user who created the project and owner';

CREATE TABLE IF NOT EXISTS public.monitorzones
(
    geom geometry,
    polyid bigserial,
    projectid integer
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE monitorzones ADD foreign key (projectid) references project ON update cascade ON delete cascade;




CREATE TABLE IF NOT EXISTS public.userproject
(
    userid integer NOT NULL,
    projectid integer NOT NULL

)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.userproject
    OWNER to postgres;




CREATE TABLE IF NOT EXISTS public.users
(
    userid serial,
    password text COLLATE pg_catalog."default" NOT NULL
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.users
    OWNER to postgres;



CREATE TABLE IF NOT EXISTS public.viirs_snpp
(
    index bigint,
    latitude double precision,
    longitude double precision,
    bright_ti4 double precision,
    scan double precision,
    track double precision,
    acq_date text COLLATE pg_catalog."default",
    acq_time bigint,
    satellite text COLLATE pg_catalog."default",
    instrument text COLLATE pg_catalog."default",
    confidence text COLLATE pg_catalog."default",
    version bigint,
    bright_ti5 double precision,
    frp double precision,
    daynight text COLLATE pg_catalog."default",
    type bigint
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.viirs_snpp
    OWNER to postgres;
-- Index: ix_viirs-snpp_index

-- DROP INDEX IF EXISTS public."ix_viirs-snpp_index";

CREATE INDEX IF NOT EXISTS "ix_viirs-snpp_index"
    ON public.viirs_snpp USING btree
    (index ASC NULLS LAST)
    TABLESPACE pg_default;


------
Alter table users add primary key (userid);

Alter table userproject add primary key (userid,projectid);
Alter table userproject add foreign key (userid) references users on update cascade on delete set null;


Alter table monitorzones add foreign key (projectid) references project on update cascade on delete cascade;

---
--- Loading Historical data
--- MODIS data load 
---
CREATE TABLE IF NOT EXISTS public.datatablesindex
(
    tablename text COLLATE pg_catalog."default" NOT NULL,
    sensor text COLLATE pg_catalog."default",
    dt_added date,
    geom_mbr geometry,
    CONSTRAINT datatablesindex_pkey PRIMARY KEY (tablename)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;


---------------------------------
-- LOAD FIRE DATA HISTORY —
---------------------------------

--- 2019

-- CREATE TABLE IF NOT EXISTS myanmar_modis_2019
-- (
   
--     latitude double precision,
--     longitude double precision,
--     brightness double precision,
--     scan double precision,
--     track double precision,
--     acq_date text COLLATE pg_catalog."default",
--     acq_time bigint,
--     satellite text COLLATE pg_catalog."default",
--     instrument text COLLATE pg_catalog."default",
--     confidence bigint,
--     version double precision,
--     bright_t31 double precision,
--     frp double precision,
--     daynight text COLLATE pg_catalog."default",
--     type bigint
-- )
-- WITH (
--     OIDS = FALSE
-- )
-- TABLESPACE pg_default;


-- copy modis FROM '/var/www/html/fd/modis/modis_2019_Myanmar.csv' delimiter ',' CSV header;


-- alter table myanmar_modis_2019 add column geom geometry;
-- update myanmar_modis_2019 set geom = st_setsrid(st_makepoint(longitude,latitude),   4326);
-- create index on myanmar_modis_2019 using gist(geom);

-- alter table myanmar_modis_2019 add column acqdate date;
-- update myanmar_modis_2019 set acqdate = acq_date::date;
-- create index on myanmar_modis_2019 using btree(acqdate);


-- insert into datatablesindex (tablename,sensor,dt_added,geom_mbr) 
-- values ('myanmar_modis_2019','modis',now(),  (select st_setsrid (st_extent(st_transform(geom,4326)),4326)  from myanmar_modis_2019); );



-- --- 2020

-- CREATE TABLE IF NOT EXISTS myanmar_modis_2020
-- (
   
--     latitude double precision,
--     longitude double precision,
--     brightness double precision,
--     scan double precision,
--     track double precision,
--     acq_date text COLLATE pg_catalog."default",
--     acq_time bigint,
--     satellite text COLLATE pg_catalog."default",
--     instrument text COLLATE pg_catalog."default",
--     confidence bigint,
--     version double precision,
--     bright_t31 double precision,
--     frp double precision,
--     daynight text COLLATE pg_catalog."default",
--     type bigint
-- )
-- WITH (
--     OIDS = FALSE
-- )
-- TABLESPACE pg_default;


-- copy myanmar_modis_2020 FROM '/var/www/html/fd/modis/modis_2020_Myanmar.csv' delimiter ',' CSV header;


-- alter table myanmar_modis_2020 add column geom geometry;
-- update myanmar_modis_2020 set geom = st_setsrid(st_makepoint(longitude,latitude),   4326);
-- create index on myanmar_modis_2020 using gist(geom);

-- alter table myanmar_modis_2020 add column acqdate date;
-- update myanmar_modis_2020 set acqdate = acq_date::date;
-- create index on myanmar_modis_2020 using btree(acqdate);


-- insert into datatablesindex (tablename,sensor,dt_added,geom_mbr) 
-- values ('myanmar_modis_2020','modis',now(),  (select st_setsrid (st_extent(st_transform(geom,4326)),4326)  from myanmar_modis_2020); );



-- --- 2021

-- CREATE TABLE IF NOT EXISTS myanmar_modis_2021
-- (
   
--     latitude double precision,
--     longitude double precision,
--     brightness double precision,
--     scan double precision,
--     track double precision,
--     acq_date text COLLATE pg_catalog."default",
--     acq_time bigint,
--     satellite text COLLATE pg_catalog."default",
--     instrument text COLLATE pg_catalog."default",
--     confidence bigint,
--     version double precision,
--     bright_t31 double precision,
--     frp double precision,
--     daynight text COLLATE pg_catalog."default",
--     type bigint
-- )
-- WITH (
--     OIDS = FALSE
-- )
-- TABLESPACE pg_default;


-- copy myanmar_modis_2021 FROM '/var/www/html/fd/modis/modis_2021_Myanmar.csv' delimiter ',' CSV header;


-- alter table myanmar_modis_2021 add column geom geometry;
-- update myanmar_modis_2021 set geom = st_setsrid(st_makepoint(longitude,latitude),   4326);
-- create index on myanmar_modis_2021 using gist(geom);

-- alter table myanmar_modis_2021 add column acqdate date;
-- update myanmar_modis_2021 set acqdate = acq_date::date;
-- create index on myanmar_modis_2021 using btree(acqdate);


-- insert into datatablesindex (tablename,sensor,dt_added,geom_mbr) 
-- values ('myanmar_modis_2021','modis',now(),  (select st_setsrid (st_extent(st_transform(geom,4326)),4326)  from myanmar_modis_2021); );

-- ------------------------------------------
-- -- for VIIRS SNPP layer



-- CREATE TABLE IF NOT EXISTS myanmar_viirs_snpp_2019
-- (
--     latitude double precision,
--     longitude double precision,
--     bright_ti4 double precision,
--     scan double precision,
--     track double precision,
--     acq_date text COLLATE pg_catalog."default",
--     acq_time bigint,
--     satellite text COLLATE pg_catalog."default",
--     instrument text COLLATE pg_catalog."default",
--     confidence text COLLATE pg_catalog."default",
--     version bigint,
--     bright_ti5 double precision,
--     frp double precision,
--     daynight text COLLATE pg_catalog."default",
--     type bigint
-- )
-- WITH (
--     OIDS = FALSE
-- )
-- TABLESPACE pg_default;


-- copy myanmar_viirs_snpp_2019 FROM '/var/www/html/fd/modis/viirs-snpp_2019_Myanmar.csv' delimiter ',' CSV header;


-- alter table myanmar_viirs_snpp_2019 add column geom geometry;
-- update myanmar_viirs_snpp_2019 set geom = st_setsrid(st_makepoint(longitude,latitude),   4326);
-- create index on myanmar_viirs_snpp_2019 using gist(geom);

-- alter table myanmar_viirs_snpp_2019 add column acqdate date;
-- update myanmar_viirs_snpp_2019 set acqdate = acq_date::date;
-- create index on myanmar_viirs_snpp_2019 using btree(acqdate);

-- insert into datatablesindex (tablename,sensor,dt_added,geom_mbr) 
-- values ('myanmar_viirs_snpp_2019','viirs_snpp',now(),  (select st_setsrid (st_extent(st_transform(geom,4326)),4326)  from myanmar_viirs_snpp_2019) );


-- --------2020


-- CREATE TABLE IF NOT EXISTS myanmar_viirs_snpp_2020
-- (
--     latitude double precision,
--     longitude double precision,
--     bright_ti4 double precision,
--     scan double precision,
--     track double precision,
--     acq_date text COLLATE pg_catalog."default",
--     acq_time bigint,
--     satellite text COLLATE pg_catalog."default",
--     instrument text COLLATE pg_catalog."default",
--     confidence text COLLATE pg_catalog."default",
--     version bigint,
--     bright_ti5 double precision,
--     frp double precision,
--     daynight text COLLATE pg_catalog."default",
--     type bigint
-- )
-- WITH (
--     OIDS = FALSE
-- )
-- TABLESPACE pg_default;


-- copy myanmar_viirs_snpp_2020 FROM '/var/www/html/fd/modis/viirs-snpp_2020_Myanmar.csv' delimiter ',' CSV header;


-- alter table myanmar_viirs_snpp_2020 add column geom geometry;
-- update myanmar_viirs_snpp_2020 set geom = st_setsrid(st_makepoint(longitude,latitude),   4326);
-- create index on myanmar_viirs_snpp_2020 using gist(geom);

-- alter table myanmar_viirs_snpp_2020 add column acqdate date;
-- update myanmar_viirs_snpp_2020 set acqdate = acq_date::date;
-- create index on myanmar_viirs_snpp_2019 using btree(acqdate);

-- insert into datatablesindex (tablename,sensor,dt_added,geom_mbr) 
-- values ('myanmar_viirs_snpp_2020','viirs_snpp',now(),  (select st_setsrid (st_extent(st_transform(geom,4326)),4326)  from myanmar_viirs_snpp_2020) );


-- ---------2021



-- CREATE TABLE IF NOT EXISTS myanmar_viirs_snpp_2021
-- (
--     latitude double precision,
--     longitude double precision,
--     bright_ti4 double precision,
--     scan double precision,
--     track double precision,
--     acq_date text COLLATE pg_catalog."default",
--     acq_time bigint,
--     satellite text COLLATE pg_catalog."default",
--     instrument text COLLATE pg_catalog."default",
--     confidence text COLLATE pg_catalog."default",
--     version bigint,
--     bright_ti5 double precision,
--     frp double precision,
--     daynight text COLLATE pg_catalog."default",
--     type bigint
-- )
-- WITH (
--     OIDS = FALSE
-- )
-- TABLESPACE pg_default;


-- copy myanmar_viirs_snpp_2021 FROM '/var/www/html/fd/modis/viirs-snpp_2021_Myanmar.csv' delimiter ',' CSV header;


-- alter table myanmar_viirs_snpp_2021 add column geom geometry;
-- update myanmar_viirs_snpp_2021 set geom = st_setsrid(st_makepoint(longitude,latitude),   4326);
-- create index on myanmar_viirs_snpp_2021 using gist(geom);

-- alter table myanmar_viirs_snpp_2021 add column acqdate date;
-- update myanmar_viirs_snpp_2021 set acqdate = acq_date::date;
-- create index on myanmar_viirs_snpp_2021 using btree(acqdate);

-- insert into datatablesindex (tablename,sensor,dt_added,geom_mbr) 
-- values ('myanmar_viirs_snpp_2021','viirs_snpp',now(),  (select st_setsrid (st_extent(st_transform(geom,4326)),4326)  from myanmar_viirs_snpp_2021) );







---
-- DB PERMISSIONS
---
REVOKE ALL
ON ALL TABLES IN SCHEMA public
FROM PUBLIC;

GRANT ALL
ON ALL TABLES
IN SCHEMA "public"
TO firemapusr;


GRANT CONNECT ON DATABASE nasafiremap TO firemapusr;

GRANT USAGE, SELECT ON SEQUENCE monitorzones_polyid_seq TO firemapusr;
GRANT USAGE, SELECT ON SEQUENCE project_projectid_seq TO firemapusr;
GRANT USAGE, SELECT ON SEQUENCE users_userid_seq TO firemapusr;


---
-- dummy data
---

-- insert into users values(101,'e105fc3091927de105f7b55c7a9c263e4e665935d441e03c060f4f2c6af58fc1');

-- insert into userproject values (101,117);
-- insert into project values ('test project','1 Jan 2022',True,117,'test@123.com',101)
