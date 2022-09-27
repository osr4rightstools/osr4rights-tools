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

CREATE  TABLE IF NOT EXISTS public.modis
(
    index bigint,
    latitude double precision,
    longitude double precision,
    brightness double precision,
    scan double precision,
    track double precision,
    acq_date date,
    acq_time bigint,
    satellite text COLLATE pg_catalog."default",
    instrument text COLLATE pg_catalog."default",
    confidence bigint,
    version double precision,
    bright_t31 double precision,
    frp double precision,
    daynight text COLLATE pg_catalog."default",
    type bigint,
    geom geometry
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

 
ALTER TABLE IF EXISTS public.modis
    OWNER to postgres;

-- Index: ix_modis_index

-- DROP INDEX IF EXISTS public.ix_modis_index;

CREATE INDEX IF NOT EXISTS ix_modis_index
    ON public.modis USING btree
    (index ASC NULLS LAST)
    TABLESPACE pg_default;

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

-- DB PERMISSIONS

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


-- dummy data

insert into users values(101,'e105fc3091927de105f7b55c7a9c263e4e665935d441e03c060f4f2c6af58fc1');

insert into userproject values (101,117);
insert into project values ('test project','1 Jan 2022',True,117,'test@123.com',101)
