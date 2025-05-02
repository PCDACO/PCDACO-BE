--
-- PostgreSQL database dump
--

-- Dumped from database version 13.5 (Debian 13.5-1.pgdg110+1)
-- Dumped by pg_dump version 13.5 (Debian 13.5-1.pgdg110+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: hangfire; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA hangfire;


ALTER SCHEMA hangfire OWNER TO postgres;

--
-- Name: postgis; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS postgis WITH SCHEMA public;


--
-- Name: EXTENSION postgis; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION postgis IS 'PostGIS geometry and geography spatial types and functions';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: aggregatedcounter; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.aggregatedcounter (
    id bigint NOT NULL,
    key text NOT NULL,
    value bigint NOT NULL,
    expireat timestamp with time zone
);


ALTER TABLE hangfire.aggregatedcounter OWNER TO postgres;

--
-- Name: aggregatedcounter_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.aggregatedcounter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE hangfire.aggregatedcounter_id_seq OWNER TO postgres;

--
-- Name: aggregatedcounter_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.aggregatedcounter_id_seq OWNED BY hangfire.aggregatedcounter.id;


--
-- Name: counter; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.counter (
    id bigint NOT NULL,
    key text NOT NULL,
    value bigint NOT NULL,
    expireat timestamp with time zone
);


ALTER TABLE hangfire.counter OWNER TO postgres;

--
-- Name: counter_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.counter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE hangfire.counter_id_seq OWNER TO postgres;

--
-- Name: counter_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.counter_id_seq OWNED BY hangfire.counter.id;


--
-- Name: hash; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.hash (
    id bigint NOT NULL,
    key text NOT NULL,
    field text NOT NULL,
    value text,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.hash OWNER TO postgres;

--
-- Name: hash_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.hash_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE hangfire.hash_id_seq OWNER TO postgres;

--
-- Name: hash_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.hash_id_seq OWNED BY hangfire.hash.id;


--
-- Name: job; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.job (
    id bigint NOT NULL,
    stateid bigint,
    statename text,
    invocationdata jsonb NOT NULL,
    arguments jsonb NOT NULL,
    createdat timestamp with time zone NOT NULL,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.job OWNER TO postgres;

--
-- Name: job_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.job_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE hangfire.job_id_seq OWNER TO postgres;

--
-- Name: job_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.job_id_seq OWNED BY hangfire.job.id;


--
-- Name: jobparameter; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.jobparameter (
    id bigint NOT NULL,
    jobid bigint NOT NULL,
    name text NOT NULL,
    value text,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.jobparameter OWNER TO postgres;

--
-- Name: jobparameter_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.jobparameter_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE hangfire.jobparameter_id_seq OWNER TO postgres;

--
-- Name: jobparameter_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.jobparameter_id_seq OWNED BY hangfire.jobparameter.id;


--
-- Name: jobqueue; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.jobqueue (
    id bigint NOT NULL,
    jobid bigint NOT NULL,
    queue text NOT NULL,
    fetchedat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.jobqueue OWNER TO postgres;

--
-- Name: jobqueue_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.jobqueue_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE hangfire.jobqueue_id_seq OWNER TO postgres;

--
-- Name: jobqueue_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.jobqueue_id_seq OWNED BY hangfire.jobqueue.id;


--
-- Name: list; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.list (
    id bigint NOT NULL,
    key text NOT NULL,
    value text,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.list OWNER TO postgres;

--
-- Name: list_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.list_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE hangfire.list_id_seq OWNER TO postgres;

--
-- Name: list_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.list_id_seq OWNED BY hangfire.list.id;


--
-- Name: lock; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.lock (
    resource text NOT NULL,
    updatecount integer DEFAULT 0 NOT NULL,
    acquired timestamp with time zone
);


ALTER TABLE hangfire.lock OWNER TO postgres;

--
-- Name: schema; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.schema (
    version integer NOT NULL
);


ALTER TABLE hangfire.schema OWNER TO postgres;

--
-- Name: server; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.server (
    id text NOT NULL,
    data jsonb,
    lastheartbeat timestamp with time zone NOT NULL,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.server OWNER TO postgres;

--
-- Name: set; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.set (
    id bigint NOT NULL,
    key text NOT NULL,
    score double precision NOT NULL,
    value text NOT NULL,
    expireat timestamp with time zone,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.set OWNER TO postgres;

--
-- Name: set_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.set_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE hangfire.set_id_seq OWNER TO postgres;

--
-- Name: set_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.set_id_seq OWNED BY hangfire.set.id;


--
-- Name: state; Type: TABLE; Schema: hangfire; Owner: postgres
--

CREATE TABLE hangfire.state (
    id bigint NOT NULL,
    jobid bigint NOT NULL,
    name text NOT NULL,
    reason text,
    createdat timestamp with time zone NOT NULL,
    data jsonb,
    updatecount integer DEFAULT 0 NOT NULL
);


ALTER TABLE hangfire.state OWNER TO postgres;

--
-- Name: state_id_seq; Type: SEQUENCE; Schema: hangfire; Owner: postgres
--

CREATE SEQUENCE hangfire.state_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE hangfire.state_id_seq OWNER TO postgres;

--
-- Name: state_id_seq; Type: SEQUENCE OWNED BY; Schema: hangfire; Owner: postgres
--

ALTER SEQUENCE hangfire.state_id_seq OWNED BY hangfire.state.id;


--
-- Name: Amenities; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Amenities" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" text NOT NULL,
    "IconUrl" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."Amenities" OWNER TO postgres;

--
-- Name: BankAccounts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."BankAccounts" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "EncryptionKeyId" uuid NOT NULL,
    "BankInfoId" uuid NOT NULL,
    "EncryptedBankAccount" text NOT NULL,
    "BankAccountName" text NOT NULL,
    "IsPrimary" boolean NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."BankAccounts" OWNER TO postgres;

--
-- Name: BankInfos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."BankInfos" (
    "Id" uuid NOT NULL,
    "BankLookUpId" uuid NOT NULL,
    "Name" text NOT NULL,
    "Code" text NOT NULL,
    "Bin" integer NOT NULL,
    "ShortName" text NOT NULL,
    "LogoUrl" text NOT NULL,
    "IconUrl" text NOT NULL,
    "SwiftCode" text NOT NULL,
    "LookupSupported" integer NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."BankInfos" OWNER TO postgres;

--
-- Name: BookingLockedBalances; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."BookingLockedBalances" (
    "Id" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "OwnerId" uuid NOT NULL,
    "Amount" numeric NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."BookingLockedBalances" OWNER TO postgres;

--
-- Name: BookingReports; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."BookingReports" (
    "Id" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "ReportedById" uuid NOT NULL,
    "Title" text NOT NULL,
    "ReportType" integer NOT NULL,
    "Description" text NOT NULL,
    "Status" integer NOT NULL,
    "CompensationPaidUserId" uuid,
    "CompensationReason" text,
    "CompensationAmount" numeric,
    "IsCompensationPaid" boolean,
    "CompensationPaidImageUrl" text,
    "CompensationPaidAt" timestamp with time zone,
    "ResolvedAt" timestamp with time zone,
    "ResolvedById" uuid,
    "ResolutionComments" text,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."BookingReports" OWNER TO postgres;

--
-- Name: Bookings; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Bookings" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CarId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "StartTime" timestamp with time zone NOT NULL,
    "EndTime" timestamp with time zone NOT NULL,
    "ActualReturnTime" timestamp with time zone NOT NULL,
    "BasePrice" numeric NOT NULL,
    "PlatformFee" numeric NOT NULL,
    "ExcessDay" numeric NOT NULL,
    "ExcessDayFee" numeric NOT NULL,
    "TotalAmount" numeric NOT NULL,
    "TotalDistance" numeric NOT NULL,
    "Note" text NOT NULL,
    "IsCarReturned" boolean NOT NULL,
    "PayOSOrderCode" bigint,
    "IsPaid" boolean NOT NULL,
    "IsRefund" boolean NOT NULL,
    "RefundAmount" numeric,
    "RefundDate" timestamp with time zone,
    "ExtensionAmount" numeric,
    "IsExtensionPaid" boolean,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."Bookings" OWNER TO postgres;

--
-- Name: CarAmenities; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CarAmenities" (
    "Id" uuid NOT NULL,
    "CarId" uuid NOT NULL,
    "AmenityId" uuid NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."CarAmenities" OWNER TO postgres;

--
-- Name: CarAvailabilities; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CarAvailabilities" (
    "Id" uuid NOT NULL,
    "CarId" uuid NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "IsAvailable" boolean NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."CarAvailabilities" OWNER TO postgres;

--
-- Name: CarContracts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CarContracts" (
    "Id" uuid NOT NULL,
    "CarId" uuid NOT NULL,
    "TechnicianId" uuid,
    "GPSDeviceId" uuid,
    "Terms" text NOT NULL,
    "OwnerSignatureDate" timestamp with time zone,
    "TechnicianSignatureDate" timestamp with time zone,
    "OwnerSignature" text,
    "TechnicianSignature" text,
    "InspectionResults" text,
    "Status" integer NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."CarContracts" OWNER TO postgres;

--
-- Name: CarGPSes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CarGPSes" (
    "Id" uuid NOT NULL,
    "DeviceId" uuid NOT NULL,
    "CarId" uuid NOT NULL,
    "Location" public.geometry NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."CarGPSes" OWNER TO postgres;

--
-- Name: CarInspections; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CarInspections" (
    "Id" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "Type" integer NOT NULL,
    "Notes" text NOT NULL,
    "IsComplete" boolean NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."CarInspections" OWNER TO postgres;

--
-- Name: CarReports; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CarReports" (
    "Id" uuid NOT NULL,
    "CarId" uuid NOT NULL,
    "ReportedById" uuid NOT NULL,
    "Title" text NOT NULL,
    "ReportType" integer NOT NULL,
    "Description" text NOT NULL,
    "Status" integer NOT NULL,
    "ResolvedAt" timestamp with time zone,
    "ResolvedById" uuid,
    "ResolutionComments" text,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."CarReports" OWNER TO postgres;

--
-- Name: CarStatistics; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CarStatistics" (
    "Id" uuid NOT NULL,
    "CarId" uuid NOT NULL,
    "TotalBooking" integer NOT NULL,
    "TotalCompleted" integer NOT NULL,
    "TotalRejected" integer NOT NULL,
    "TotalExpired" integer NOT NULL,
    "TotalCancelled" integer NOT NULL,
    "TotalEarning" numeric NOT NULL,
    "TotalDistance" numeric NOT NULL,
    "AverageRating" numeric NOT NULL,
    "LastRented" timestamp with time zone,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."CarStatistics" OWNER TO postgres;

--
-- Name: Cars; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Cars" (
    "Id" uuid NOT NULL,
    "OwnerId" uuid NOT NULL,
    "ModelId" uuid NOT NULL,
    "FuelTypeId" uuid NOT NULL,
    "TransmissionTypeId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "LicensePlate" text NOT NULL,
    "Color" text NOT NULL,
    "Seat" integer NOT NULL,
    "Description" text NOT NULL,
    "FuelConsumption" numeric NOT NULL,
    "RequiresCollateral" boolean NOT NULL,
    "Price" numeric NOT NULL,
    "Terms" text NOT NULL,
    "TotalRented" integer NOT NULL,
    "TotalEarning" numeric NOT NULL,
    "PickupLocation" public.geometry NOT NULL,
    "PickupAddress" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."Cars" OWNER TO postgres;

--
-- Name: Contracts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Contracts" (
    "Id" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "Terms" text NOT NULL,
    "DriverSignatureDate" timestamp with time zone,
    "OwnerSignatureDate" timestamp with time zone,
    "DriverSignature" text,
    "OwnerSignature" text,
    "PickupAddress" text,
    "RentalPrice" numeric NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."Contracts" OWNER TO postgres;

--
-- Name: EncryptionKeys; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."EncryptionKeys" (
    "Id" uuid NOT NULL,
    "EncryptedKey" text NOT NULL,
    "IV" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."EncryptionKeys" OWNER TO postgres;

--
-- Name: Feedbacks; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Feedbacks" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "Point" integer NOT NULL,
    "Content" text NOT NULL,
    "Type" integer NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."Feedbacks" OWNER TO postgres;

--
-- Name: FuelTypes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."FuelTypes" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."FuelTypes" OWNER TO postgres;

--
-- Name: GPSDevices; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."GPSDevices" (
    "Id" uuid NOT NULL,
    "Status" integer NOT NULL,
    "OSBuildId" text NOT NULL,
    "Name" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."GPSDevices" OWNER TO postgres;

--
-- Name: ImageCars; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ImageCars" (
    "Id" uuid NOT NULL,
    "CarId" uuid NOT NULL,
    "TypeId" uuid NOT NULL,
    "Url" text NOT NULL,
    "Name" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."ImageCars" OWNER TO postgres;

--
-- Name: ImageFeedbacks; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ImageFeedbacks" (
    "Id" uuid NOT NULL,
    "FeedbackId" uuid NOT NULL,
    "Url" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."ImageFeedbacks" OWNER TO postgres;

--
-- Name: ImageReports; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ImageReports" (
    "Id" uuid NOT NULL,
    "BookingReportId" uuid,
    "CarReportId" uuid,
    "Url" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."ImageReports" OWNER TO postgres;

--
-- Name: ImageTypes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ImageTypes" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."ImageTypes" OWNER TO postgres;

--
-- Name: InspectionPhotos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."InspectionPhotos" (
    "Id" uuid NOT NULL,
    "InspectionId" uuid,
    "ScheduleId" uuid,
    "Type" integer NOT NULL,
    "PhotoUrl" text NOT NULL,
    "Description" text NOT NULL,
    "InspectionCertificateExpiryDate" timestamp with time zone,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."InspectionPhotos" OWNER TO postgres;

--
-- Name: InspectionSchedules; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."InspectionSchedules" (
    "Id" uuid NOT NULL,
    "TechnicianId" uuid NOT NULL,
    "CarId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "Note" text NOT NULL,
    "InspectionAddress" text NOT NULL,
    "InspectionDate" timestamp with time zone NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "Type" integer NOT NULL,
    "ReportId" uuid,
    "CarReportId" uuid,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."InspectionSchedules" OWNER TO postgres;

--
-- Name: Manufacturers; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Manufacturers" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "LogoUrl" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."Manufacturers" OWNER TO postgres;

--
-- Name: Models; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Models" (
    "Id" uuid NOT NULL,
    "ManufacturerId" uuid NOT NULL,
    "Name" text NOT NULL,
    "ReleaseDate" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."Models" OWNER TO postgres;

--
-- Name: RefreshTokens; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RefreshTokens" (
    "Token" text NOT NULL,
    "UserId" uuid NOT NULL,
    "ExpiryDate" timestamp with time zone NOT NULL,
    "IsUsed" boolean NOT NULL,
    "IsRevoked" boolean NOT NULL,
    "RevokedAt" timestamp with time zone
);


ALTER TABLE public."RefreshTokens" OWNER TO postgres;

--
-- Name: TransactionTypes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."TransactionTypes" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."TransactionTypes" OWNER TO postgres;

--
-- Name: Transactions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Transactions" (
    "Id" uuid NOT NULL,
    "FromUserId" uuid NOT NULL,
    "ToUserId" uuid NOT NULL,
    "BookingId" uuid,
    "BankAccountId" uuid,
    "TypeId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "Amount" numeric NOT NULL,
    "Description" text NOT NULL,
    "BalanceAfter" numeric NOT NULL,
    "ProofUrl" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."Transactions" OWNER TO postgres;

--
-- Name: TransmissionTypes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."TransmissionTypes" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."TransmissionTypes" OWNER TO postgres;

--
-- Name: TripTrackings; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."TripTrackings" (
    "Id" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "Location" public.geometry NOT NULL,
    "Distance" numeric NOT NULL,
    "CumulativeDistance" numeric NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."TripTrackings" OWNER TO postgres;

--
-- Name: UserRoles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."UserRoles" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."UserRoles" OWNER TO postgres;

--
-- Name: UserStatistics; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."UserStatistics" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TotalBooking" integer NOT NULL,
    "TotalCompleted" integer NOT NULL,
    "TotalRejected" integer NOT NULL,
    "TotalExpired" integer NOT NULL,
    "TotalCancelled" integer NOT NULL,
    "TotalEarning" numeric NOT NULL,
    "AverageRating" numeric NOT NULL,
    "TotalCreatedInspectionSchedule" integer NOT NULL,
    "TotalApprovedInspectionSchedule" integer NOT NULL,
    "TotalRejectedInspectionSchedule" integer NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."UserStatistics" OWNER TO postgres;

--
-- Name: Users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Users" (
    "Id" uuid NOT NULL,
    "EncryptionKeyId" uuid NOT NULL,
    "RoleId" uuid NOT NULL,
    "AvatarUrl" text NOT NULL,
    "Name" text NOT NULL,
    "Email" text NOT NULL,
    "Password" text NOT NULL,
    "Address" text NOT NULL,
    "DateOfBirth" timestamp with time zone NOT NULL,
    "Phone" text NOT NULL,
    "Balance" numeric NOT NULL,
    "LockedBalance" numeric NOT NULL,
    "EncryptedLicenseNumber" text NOT NULL,
    "LicenseImageFrontUrl" text NOT NULL,
    "LicenseImageBackUrl" text NOT NULL,
    "LicenseExpiryDate" timestamp with time zone,
    "LicenseIsApproved" boolean,
    "LicenseRejectReason" text,
    "LicenseImageUploadedAt" timestamp with time zone,
    "LicenseApprovedAt" timestamp with time zone,
    "IsBanned" boolean NOT NULL,
    "BannedReason" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."Users" OWNER TO postgres;

--
-- Name: WithdrawalRequests; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."WithdrawalRequests" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "BankAccountId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "Amount" numeric NOT NULL,
    "RejectReason" text NOT NULL,
    "TransactionId" uuid,
    "ProcessedAt" timestamp with time zone,
    "ProcessedByAdminId" uuid,
    "AdminNote" text,
    "UpdatedAt" timestamp with time zone,
    "DeletedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL
);


ALTER TABLE public."WithdrawalRequests" OWNER TO postgres;

--
-- Name: aggregatedcounter id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.aggregatedcounter ALTER COLUMN id SET DEFAULT nextval('hangfire.aggregatedcounter_id_seq'::regclass);


--
-- Name: counter id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.counter ALTER COLUMN id SET DEFAULT nextval('hangfire.counter_id_seq'::regclass);


--
-- Name: hash id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.hash ALTER COLUMN id SET DEFAULT nextval('hangfire.hash_id_seq'::regclass);


--
-- Name: job id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.job ALTER COLUMN id SET DEFAULT nextval('hangfire.job_id_seq'::regclass);


--
-- Name: jobparameter id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobparameter ALTER COLUMN id SET DEFAULT nextval('hangfire.jobparameter_id_seq'::regclass);


--
-- Name: jobqueue id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobqueue ALTER COLUMN id SET DEFAULT nextval('hangfire.jobqueue_id_seq'::regclass);


--
-- Name: list id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.list ALTER COLUMN id SET DEFAULT nextval('hangfire.list_id_seq'::regclass);


--
-- Name: set id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.set ALTER COLUMN id SET DEFAULT nextval('hangfire.set_id_seq'::regclass);


--
-- Name: state id; Type: DEFAULT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.state ALTER COLUMN id SET DEFAULT nextval('hangfire.state_id_seq'::regclass);


--
-- Name: aggregatedcounter aggregatedcounter_key_key; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.aggregatedcounter
    ADD CONSTRAINT aggregatedcounter_key_key UNIQUE (key);


--
-- Name: aggregatedcounter aggregatedcounter_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.aggregatedcounter
    ADD CONSTRAINT aggregatedcounter_pkey PRIMARY KEY (id);


--
-- Name: counter counter_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.counter
    ADD CONSTRAINT counter_pkey PRIMARY KEY (id);


--
-- Name: hash hash_key_field_key; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.hash
    ADD CONSTRAINT hash_key_field_key UNIQUE (key, field);


--
-- Name: hash hash_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.hash
    ADD CONSTRAINT hash_pkey PRIMARY KEY (id);


--
-- Name: job job_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.job
    ADD CONSTRAINT job_pkey PRIMARY KEY (id);


--
-- Name: jobparameter jobparameter_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobparameter
    ADD CONSTRAINT jobparameter_pkey PRIMARY KEY (id);


--
-- Name: jobqueue jobqueue_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobqueue
    ADD CONSTRAINT jobqueue_pkey PRIMARY KEY (id);


--
-- Name: list list_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.list
    ADD CONSTRAINT list_pkey PRIMARY KEY (id);


--
-- Name: lock lock_resource_key; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.lock
    ADD CONSTRAINT lock_resource_key UNIQUE (resource);

ALTER TABLE ONLY hangfire.lock REPLICA IDENTITY USING INDEX lock_resource_key;


--
-- Name: schema schema_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.schema
    ADD CONSTRAINT schema_pkey PRIMARY KEY (version);


--
-- Name: server server_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.server
    ADD CONSTRAINT server_pkey PRIMARY KEY (id);


--
-- Name: set set_key_value_key; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.set
    ADD CONSTRAINT set_key_value_key UNIQUE (key, value);


--
-- Name: set set_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.set
    ADD CONSTRAINT set_pkey PRIMARY KEY (id);


--
-- Name: state state_pkey; Type: CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.state
    ADD CONSTRAINT state_pkey PRIMARY KEY (id);


--
-- Name: Amenities PK_Amenities; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Amenities"
    ADD CONSTRAINT "PK_Amenities" PRIMARY KEY ("Id");


--
-- Name: BankAccounts PK_BankAccounts; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BankAccounts"
    ADD CONSTRAINT "PK_BankAccounts" PRIMARY KEY ("Id");


--
-- Name: BankInfos PK_BankInfos; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BankInfos"
    ADD CONSTRAINT "PK_BankInfos" PRIMARY KEY ("Id");


--
-- Name: BookingLockedBalances PK_BookingLockedBalances; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BookingLockedBalances"
    ADD CONSTRAINT "PK_BookingLockedBalances" PRIMARY KEY ("Id");


--
-- Name: BookingReports PK_BookingReports; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BookingReports"
    ADD CONSTRAINT "PK_BookingReports" PRIMARY KEY ("Id");


--
-- Name: Bookings PK_Bookings; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Bookings"
    ADD CONSTRAINT "PK_Bookings" PRIMARY KEY ("Id");


--
-- Name: CarAmenities PK_CarAmenities; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarAmenities"
    ADD CONSTRAINT "PK_CarAmenities" PRIMARY KEY ("Id");


--
-- Name: CarAvailabilities PK_CarAvailabilities; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarAvailabilities"
    ADD CONSTRAINT "PK_CarAvailabilities" PRIMARY KEY ("Id");


--
-- Name: CarContracts PK_CarContracts; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarContracts"
    ADD CONSTRAINT "PK_CarContracts" PRIMARY KEY ("Id");


--
-- Name: CarGPSes PK_CarGPSes; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarGPSes"
    ADD CONSTRAINT "PK_CarGPSes" PRIMARY KEY ("Id");


--
-- Name: CarInspections PK_CarInspections; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarInspections"
    ADD CONSTRAINT "PK_CarInspections" PRIMARY KEY ("Id");


--
-- Name: CarReports PK_CarReports; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarReports"
    ADD CONSTRAINT "PK_CarReports" PRIMARY KEY ("Id");


--
-- Name: CarStatistics PK_CarStatistics; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarStatistics"
    ADD CONSTRAINT "PK_CarStatistics" PRIMARY KEY ("Id");


--
-- Name: Cars PK_Cars; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Cars"
    ADD CONSTRAINT "PK_Cars" PRIMARY KEY ("Id");


--
-- Name: Contracts PK_Contracts; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Contracts"
    ADD CONSTRAINT "PK_Contracts" PRIMARY KEY ("Id");


--
-- Name: EncryptionKeys PK_EncryptionKeys; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EncryptionKeys"
    ADD CONSTRAINT "PK_EncryptionKeys" PRIMARY KEY ("Id");


--
-- Name: Feedbacks PK_Feedbacks; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Feedbacks"
    ADD CONSTRAINT "PK_Feedbacks" PRIMARY KEY ("Id");


--
-- Name: FuelTypes PK_FuelTypes; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."FuelTypes"
    ADD CONSTRAINT "PK_FuelTypes" PRIMARY KEY ("Id");


--
-- Name: GPSDevices PK_GPSDevices; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."GPSDevices"
    ADD CONSTRAINT "PK_GPSDevices" PRIMARY KEY ("Id");


--
-- Name: ImageCars PK_ImageCars; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ImageCars"
    ADD CONSTRAINT "PK_ImageCars" PRIMARY KEY ("Id");


--
-- Name: ImageFeedbacks PK_ImageFeedbacks; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ImageFeedbacks"
    ADD CONSTRAINT "PK_ImageFeedbacks" PRIMARY KEY ("Id");


--
-- Name: ImageReports PK_ImageReports; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ImageReports"
    ADD CONSTRAINT "PK_ImageReports" PRIMARY KEY ("Id");


--
-- Name: ImageTypes PK_ImageTypes; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ImageTypes"
    ADD CONSTRAINT "PK_ImageTypes" PRIMARY KEY ("Id");


--
-- Name: InspectionPhotos PK_InspectionPhotos; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InspectionPhotos"
    ADD CONSTRAINT "PK_InspectionPhotos" PRIMARY KEY ("Id");


--
-- Name: InspectionSchedules PK_InspectionSchedules; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InspectionSchedules"
    ADD CONSTRAINT "PK_InspectionSchedules" PRIMARY KEY ("Id");


--
-- Name: Manufacturers PK_Manufacturers; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Manufacturers"
    ADD CONSTRAINT "PK_Manufacturers" PRIMARY KEY ("Id");


--
-- Name: Models PK_Models; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Models"
    ADD CONSTRAINT "PK_Models" PRIMARY KEY ("Id");


--
-- Name: RefreshTokens PK_RefreshTokens; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Token");


--
-- Name: TransactionTypes PK_TransactionTypes; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."TransactionTypes"
    ADD CONSTRAINT "PK_TransactionTypes" PRIMARY KEY ("Id");


--
-- Name: Transactions PK_Transactions; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "PK_Transactions" PRIMARY KEY ("Id");


--
-- Name: TransmissionTypes PK_TransmissionTypes; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."TransmissionTypes"
    ADD CONSTRAINT "PK_TransmissionTypes" PRIMARY KEY ("Id");


--
-- Name: TripTrackings PK_TripTrackings; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."TripTrackings"
    ADD CONSTRAINT "PK_TripTrackings" PRIMARY KEY ("Id");


--
-- Name: UserRoles PK_UserRoles; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."UserRoles"
    ADD CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id");


--
-- Name: UserStatistics PK_UserStatistics; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."UserStatistics"
    ADD CONSTRAINT "PK_UserStatistics" PRIMARY KEY ("Id");


--
-- Name: Users PK_Users; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");


--
-- Name: WithdrawalRequests PK_WithdrawalRequests; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."WithdrawalRequests"
    ADD CONSTRAINT "PK_WithdrawalRequests" PRIMARY KEY ("Id");


--
-- Name: ix_hangfire_counter_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_counter_expireat ON hangfire.counter USING btree (expireat);


--
-- Name: ix_hangfire_counter_key; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_counter_key ON hangfire.counter USING btree (key);


--
-- Name: ix_hangfire_hash_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_hash_expireat ON hangfire.hash USING btree (expireat);


--
-- Name: ix_hangfire_job_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_job_expireat ON hangfire.job USING btree (expireat);


--
-- Name: ix_hangfire_job_statename; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_job_statename ON hangfire.job USING btree (statename);


--
-- Name: ix_hangfire_job_statename_is_not_null; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_job_statename_is_not_null ON hangfire.job USING btree (statename) INCLUDE (id) WHERE (statename IS NOT NULL);


--
-- Name: ix_hangfire_jobparameter_jobidandname; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_jobparameter_jobidandname ON hangfire.jobparameter USING btree (jobid, name);


--
-- Name: ix_hangfire_jobqueue_fetchedat_queue_jobid; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_jobqueue_fetchedat_queue_jobid ON hangfire.jobqueue USING btree (fetchedat NULLS FIRST, queue, jobid);


--
-- Name: ix_hangfire_jobqueue_jobidandqueue; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_jobqueue_jobidandqueue ON hangfire.jobqueue USING btree (jobid, queue);


--
-- Name: ix_hangfire_jobqueue_queueandfetchedat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_jobqueue_queueandfetchedat ON hangfire.jobqueue USING btree (queue, fetchedat);


--
-- Name: ix_hangfire_list_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_list_expireat ON hangfire.list USING btree (expireat);


--
-- Name: ix_hangfire_set_expireat; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_set_expireat ON hangfire.set USING btree (expireat);


--
-- Name: ix_hangfire_set_key_score; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_set_key_score ON hangfire.set USING btree (key, score);


--
-- Name: ix_hangfire_state_jobid; Type: INDEX; Schema: hangfire; Owner: postgres
--

CREATE INDEX ix_hangfire_state_jobid ON hangfire.state USING btree (jobid);


--
-- Name: IX_BankAccounts_BankInfoId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_BankAccounts_BankInfoId" ON public."BankAccounts" USING btree ("BankInfoId");


--
-- Name: IX_BankAccounts_EncryptionKeyId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_BankAccounts_EncryptionKeyId" ON public."BankAccounts" USING btree ("EncryptionKeyId");


--
-- Name: IX_BankAccounts_UserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_BankAccounts_UserId" ON public."BankAccounts" USING btree ("UserId");


--
-- Name: IX_BookingLockedBalances_BookingId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_BookingLockedBalances_BookingId" ON public."BookingLockedBalances" USING btree ("BookingId");


--
-- Name: IX_BookingLockedBalances_OwnerId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_BookingLockedBalances_OwnerId" ON public."BookingLockedBalances" USING btree ("OwnerId");


--
-- Name: IX_BookingReports_BookingId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_BookingReports_BookingId" ON public."BookingReports" USING btree ("BookingId");


--
-- Name: IX_BookingReports_CompensationPaidUserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_BookingReports_CompensationPaidUserId" ON public."BookingReports" USING btree ("CompensationPaidUserId");


--
-- Name: IX_BookingReports_ReportedById; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_BookingReports_ReportedById" ON public."BookingReports" USING btree ("ReportedById");


--
-- Name: IX_BookingReports_ResolvedById; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_BookingReports_ResolvedById" ON public."BookingReports" USING btree ("ResolvedById");


--
-- Name: IX_Bookings_CarId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Bookings_CarId" ON public."Bookings" USING btree ("CarId");


--
-- Name: IX_Bookings_UserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Bookings_UserId" ON public."Bookings" USING btree ("UserId");


--
-- Name: IX_CarAmenities_AmenityId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_CarAmenities_AmenityId" ON public."CarAmenities" USING btree ("AmenityId");


--
-- Name: IX_CarAmenities_CarId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_CarAmenities_CarId" ON public."CarAmenities" USING btree ("CarId");


--
-- Name: IX_CarAvailabilities_CarId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_CarAvailabilities_CarId" ON public."CarAvailabilities" USING btree ("CarId");


--
-- Name: IX_CarContracts_CarId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_CarContracts_CarId" ON public."CarContracts" USING btree ("CarId");


--
-- Name: IX_CarContracts_GPSDeviceId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_CarContracts_GPSDeviceId" ON public."CarContracts" USING btree ("GPSDeviceId");


--
-- Name: IX_CarContracts_TechnicianId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_CarContracts_TechnicianId" ON public."CarContracts" USING btree ("TechnicianId");


--
-- Name: IX_CarGPSes_CarId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_CarGPSes_CarId" ON public."CarGPSes" USING btree ("CarId");


--
-- Name: IX_CarGPSes_DeviceId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_CarGPSes_DeviceId" ON public."CarGPSes" USING btree ("DeviceId");


--
-- Name: IX_CarInspections_BookingId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_CarInspections_BookingId" ON public."CarInspections" USING btree ("BookingId");


--
-- Name: IX_CarReports_CarId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_CarReports_CarId" ON public."CarReports" USING btree ("CarId");


--
-- Name: IX_CarReports_ReportedById; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_CarReports_ReportedById" ON public."CarReports" USING btree ("ReportedById");


--
-- Name: IX_CarReports_ResolvedById; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_CarReports_ResolvedById" ON public."CarReports" USING btree ("ResolvedById");


--
-- Name: IX_CarStatistics_CarId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_CarStatistics_CarId" ON public."CarStatistics" USING btree ("CarId");


--
-- Name: IX_Cars_FuelTypeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Cars_FuelTypeId" ON public."Cars" USING btree ("FuelTypeId");


--
-- Name: IX_Cars_ModelId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Cars_ModelId" ON public."Cars" USING btree ("ModelId");


--
-- Name: IX_Cars_OwnerId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Cars_OwnerId" ON public."Cars" USING btree ("OwnerId");


--
-- Name: IX_Cars_TransmissionTypeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Cars_TransmissionTypeId" ON public."Cars" USING btree ("TransmissionTypeId");


--
-- Name: IX_Contracts_BookingId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Contracts_BookingId" ON public."Contracts" USING btree ("BookingId");


--
-- Name: IX_Feedbacks_BookingId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Feedbacks_BookingId" ON public."Feedbacks" USING btree ("BookingId");


--
-- Name: IX_Feedbacks_UserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Feedbacks_UserId" ON public."Feedbacks" USING btree ("UserId");


--
-- Name: IX_ImageCars_CarId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ImageCars_CarId" ON public."ImageCars" USING btree ("CarId");


--
-- Name: IX_ImageCars_TypeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ImageCars_TypeId" ON public."ImageCars" USING btree ("TypeId");


--
-- Name: IX_ImageFeedbacks_FeedbackId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ImageFeedbacks_FeedbackId" ON public."ImageFeedbacks" USING btree ("FeedbackId");


--
-- Name: IX_ImageReports_BookingReportId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ImageReports_BookingReportId" ON public."ImageReports" USING btree ("BookingReportId");


--
-- Name: IX_ImageReports_CarReportId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ImageReports_CarReportId" ON public."ImageReports" USING btree ("CarReportId");


--
-- Name: IX_InspectionPhotos_InspectionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_InspectionPhotos_InspectionId" ON public."InspectionPhotos" USING btree ("InspectionId");


--
-- Name: IX_InspectionPhotos_ScheduleId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_InspectionPhotos_ScheduleId" ON public."InspectionPhotos" USING btree ("ScheduleId");


--
-- Name: IX_InspectionSchedules_CarId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_InspectionSchedules_CarId" ON public."InspectionSchedules" USING btree ("CarId");


--
-- Name: IX_InspectionSchedules_CarReportId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_InspectionSchedules_CarReportId" ON public."InspectionSchedules" USING btree ("CarReportId");


--
-- Name: IX_InspectionSchedules_CreatedBy; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_InspectionSchedules_CreatedBy" ON public."InspectionSchedules" USING btree ("CreatedBy");


--
-- Name: IX_InspectionSchedules_ReportId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_InspectionSchedules_ReportId" ON public."InspectionSchedules" USING btree ("ReportId");


--
-- Name: IX_InspectionSchedules_TechnicianId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_InspectionSchedules_TechnicianId" ON public."InspectionSchedules" USING btree ("TechnicianId");


--
-- Name: IX_Models_ManufacturerId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Models_ManufacturerId" ON public."Models" USING btree ("ManufacturerId");


--
-- Name: IX_RefreshTokens_UserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RefreshTokens_UserId" ON public."RefreshTokens" USING btree ("UserId");


--
-- Name: IX_Transactions_BankAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Transactions_BankAccountId" ON public."Transactions" USING btree ("BankAccountId");


--
-- Name: IX_Transactions_BookingId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Transactions_BookingId" ON public."Transactions" USING btree ("BookingId");


--
-- Name: IX_Transactions_FromUserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Transactions_FromUserId" ON public."Transactions" USING btree ("FromUserId");


--
-- Name: IX_Transactions_ToUserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Transactions_ToUserId" ON public."Transactions" USING btree ("ToUserId");


--
-- Name: IX_Transactions_TypeId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Transactions_TypeId" ON public."Transactions" USING btree ("TypeId");


--
-- Name: IX_TripTrackings_BookingId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_TripTrackings_BookingId" ON public."TripTrackings" USING btree ("BookingId");


--
-- Name: IX_UserStatistics_UserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_UserStatistics_UserId" ON public."UserStatistics" USING btree ("UserId");


--
-- Name: IX_Users_EncryptionKeyId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Users_EncryptionKeyId" ON public."Users" USING btree ("EncryptionKeyId");


--
-- Name: IX_Users_RoleId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Users_RoleId" ON public."Users" USING btree ("RoleId");


--
-- Name: IX_WithdrawalRequests_BankAccountId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_WithdrawalRequests_BankAccountId" ON public."WithdrawalRequests" USING btree ("BankAccountId");


--
-- Name: IX_WithdrawalRequests_ProcessedByAdminId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_WithdrawalRequests_ProcessedByAdminId" ON public."WithdrawalRequests" USING btree ("ProcessedByAdminId");


--
-- Name: IX_WithdrawalRequests_TransactionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_WithdrawalRequests_TransactionId" ON public."WithdrawalRequests" USING btree ("TransactionId");


--
-- Name: IX_WithdrawalRequests_UserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_WithdrawalRequests_UserId" ON public."WithdrawalRequests" USING btree ("UserId");


--
-- Name: jobparameter jobparameter_jobid_fkey; Type: FK CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.jobparameter
    ADD CONSTRAINT jobparameter_jobid_fkey FOREIGN KEY (jobid) REFERENCES hangfire.job(id) ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: state state_jobid_fkey; Type: FK CONSTRAINT; Schema: hangfire; Owner: postgres
--

ALTER TABLE ONLY hangfire.state
    ADD CONSTRAINT state_jobid_fkey FOREIGN KEY (jobid) REFERENCES hangfire.job(id) ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: BankAccounts FK_BankAccounts_BankInfos_BankInfoId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BankAccounts"
    ADD CONSTRAINT "FK_BankAccounts_BankInfos_BankInfoId" FOREIGN KEY ("BankInfoId") REFERENCES public."BankInfos"("Id") ON DELETE CASCADE;


--
-- Name: BankAccounts FK_BankAccounts_EncryptionKeys_EncryptionKeyId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BankAccounts"
    ADD CONSTRAINT "FK_BankAccounts_EncryptionKeys_EncryptionKeyId" FOREIGN KEY ("EncryptionKeyId") REFERENCES public."EncryptionKeys"("Id") ON DELETE CASCADE;


--
-- Name: BankAccounts FK_BankAccounts_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BankAccounts"
    ADD CONSTRAINT "FK_BankAccounts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: BookingLockedBalances FK_BookingLockedBalances_Bookings_BookingId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BookingLockedBalances"
    ADD CONSTRAINT "FK_BookingLockedBalances_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES public."Bookings"("Id") ON DELETE CASCADE;


--
-- Name: BookingLockedBalances FK_BookingLockedBalances_Users_OwnerId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BookingLockedBalances"
    ADD CONSTRAINT "FK_BookingLockedBalances_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: BookingReports FK_BookingReports_Bookings_BookingId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BookingReports"
    ADD CONSTRAINT "FK_BookingReports_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES public."Bookings"("Id") ON DELETE CASCADE;


--
-- Name: BookingReports FK_BookingReports_Users_CompensationPaidUserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BookingReports"
    ADD CONSTRAINT "FK_BookingReports_Users_CompensationPaidUserId" FOREIGN KEY ("CompensationPaidUserId") REFERENCES public."Users"("Id");


--
-- Name: BookingReports FK_BookingReports_Users_ReportedById; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BookingReports"
    ADD CONSTRAINT "FK_BookingReports_Users_ReportedById" FOREIGN KEY ("ReportedById") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: BookingReports FK_BookingReports_Users_ResolvedById; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."BookingReports"
    ADD CONSTRAINT "FK_BookingReports_Users_ResolvedById" FOREIGN KEY ("ResolvedById") REFERENCES public."Users"("Id");


--
-- Name: Bookings FK_Bookings_Cars_CarId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Bookings"
    ADD CONSTRAINT "FK_Bookings_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES public."Cars"("Id") ON DELETE CASCADE;


--
-- Name: Bookings FK_Bookings_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Bookings"
    ADD CONSTRAINT "FK_Bookings_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: CarAmenities FK_CarAmenities_Amenities_AmenityId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarAmenities"
    ADD CONSTRAINT "FK_CarAmenities_Amenities_AmenityId" FOREIGN KEY ("AmenityId") REFERENCES public."Amenities"("Id") ON DELETE CASCADE;


--
-- Name: CarAmenities FK_CarAmenities_Cars_CarId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarAmenities"
    ADD CONSTRAINT "FK_CarAmenities_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES public."Cars"("Id") ON DELETE CASCADE;


--
-- Name: CarAvailabilities FK_CarAvailabilities_Cars_CarId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarAvailabilities"
    ADD CONSTRAINT "FK_CarAvailabilities_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES public."Cars"("Id") ON DELETE CASCADE;


--
-- Name: CarContracts FK_CarContracts_Cars_CarId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarContracts"
    ADD CONSTRAINT "FK_CarContracts_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES public."Cars"("Id") ON DELETE CASCADE;


--
-- Name: CarContracts FK_CarContracts_GPSDevices_GPSDeviceId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarContracts"
    ADD CONSTRAINT "FK_CarContracts_GPSDevices_GPSDeviceId" FOREIGN KEY ("GPSDeviceId") REFERENCES public."GPSDevices"("Id");


--
-- Name: CarContracts FK_CarContracts_Users_TechnicianId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarContracts"
    ADD CONSTRAINT "FK_CarContracts_Users_TechnicianId" FOREIGN KEY ("TechnicianId") REFERENCES public."Users"("Id");


--
-- Name: CarGPSes FK_CarGPSes_Cars_CarId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarGPSes"
    ADD CONSTRAINT "FK_CarGPSes_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES public."Cars"("Id") ON DELETE CASCADE;


--
-- Name: CarGPSes FK_CarGPSes_GPSDevices_DeviceId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarGPSes"
    ADD CONSTRAINT "FK_CarGPSes_GPSDevices_DeviceId" FOREIGN KEY ("DeviceId") REFERENCES public."GPSDevices"("Id") ON DELETE CASCADE;


--
-- Name: CarInspections FK_CarInspections_Bookings_BookingId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarInspections"
    ADD CONSTRAINT "FK_CarInspections_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES public."Bookings"("Id") ON DELETE CASCADE;


--
-- Name: CarReports FK_CarReports_Cars_CarId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarReports"
    ADD CONSTRAINT "FK_CarReports_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES public."Cars"("Id") ON DELETE CASCADE;


--
-- Name: CarReports FK_CarReports_Users_ReportedById; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarReports"
    ADD CONSTRAINT "FK_CarReports_Users_ReportedById" FOREIGN KEY ("ReportedById") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: CarReports FK_CarReports_Users_ResolvedById; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarReports"
    ADD CONSTRAINT "FK_CarReports_Users_ResolvedById" FOREIGN KEY ("ResolvedById") REFERENCES public."Users"("Id");


--
-- Name: CarStatistics FK_CarStatistics_Cars_CarId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CarStatistics"
    ADD CONSTRAINT "FK_CarStatistics_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES public."Cars"("Id") ON DELETE CASCADE;


--
-- Name: Cars FK_Cars_FuelTypes_FuelTypeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Cars"
    ADD CONSTRAINT "FK_Cars_FuelTypes_FuelTypeId" FOREIGN KEY ("FuelTypeId") REFERENCES public."FuelTypes"("Id") ON DELETE CASCADE;


--
-- Name: Cars FK_Cars_Models_ModelId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Cars"
    ADD CONSTRAINT "FK_Cars_Models_ModelId" FOREIGN KEY ("ModelId") REFERENCES public."Models"("Id") ON DELETE CASCADE;


--
-- Name: Cars FK_Cars_TransmissionTypes_TransmissionTypeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Cars"
    ADD CONSTRAINT "FK_Cars_TransmissionTypes_TransmissionTypeId" FOREIGN KEY ("TransmissionTypeId") REFERENCES public."TransmissionTypes"("Id") ON DELETE CASCADE;


--
-- Name: Cars FK_Cars_Users_OwnerId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Cars"
    ADD CONSTRAINT "FK_Cars_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Contracts FK_Contracts_Bookings_BookingId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Contracts"
    ADD CONSTRAINT "FK_Contracts_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES public."Bookings"("Id") ON DELETE CASCADE;


--
-- Name: Feedbacks FK_Feedbacks_Bookings_BookingId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Feedbacks"
    ADD CONSTRAINT "FK_Feedbacks_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES public."Bookings"("Id") ON DELETE CASCADE;


--
-- Name: Feedbacks FK_Feedbacks_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Feedbacks"
    ADD CONSTRAINT "FK_Feedbacks_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: ImageCars FK_ImageCars_Cars_CarId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ImageCars"
    ADD CONSTRAINT "FK_ImageCars_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES public."Cars"("Id") ON DELETE CASCADE;


--
-- Name: ImageCars FK_ImageCars_ImageTypes_TypeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ImageCars"
    ADD CONSTRAINT "FK_ImageCars_ImageTypes_TypeId" FOREIGN KEY ("TypeId") REFERENCES public."ImageTypes"("Id") ON DELETE CASCADE;


--
-- Name: ImageFeedbacks FK_ImageFeedbacks_Feedbacks_FeedbackId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ImageFeedbacks"
    ADD CONSTRAINT "FK_ImageFeedbacks_Feedbacks_FeedbackId" FOREIGN KEY ("FeedbackId") REFERENCES public."Feedbacks"("Id") ON DELETE CASCADE;


--
-- Name: ImageReports FK_ImageReports_BookingReports_BookingReportId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ImageReports"
    ADD CONSTRAINT "FK_ImageReports_BookingReports_BookingReportId" FOREIGN KEY ("BookingReportId") REFERENCES public."BookingReports"("Id");


--
-- Name: ImageReports FK_ImageReports_CarReports_CarReportId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ImageReports"
    ADD CONSTRAINT "FK_ImageReports_CarReports_CarReportId" FOREIGN KEY ("CarReportId") REFERENCES public."CarReports"("Id");


--
-- Name: InspectionPhotos FK_InspectionPhotos_CarInspections_InspectionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InspectionPhotos"
    ADD CONSTRAINT "FK_InspectionPhotos_CarInspections_InspectionId" FOREIGN KEY ("InspectionId") REFERENCES public."CarInspections"("Id");


--
-- Name: InspectionPhotos FK_InspectionPhotos_InspectionSchedules_ScheduleId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InspectionPhotos"
    ADD CONSTRAINT "FK_InspectionPhotos_InspectionSchedules_ScheduleId" FOREIGN KEY ("ScheduleId") REFERENCES public."InspectionSchedules"("Id");


--
-- Name: InspectionSchedules FK_InspectionSchedules_BookingReports_ReportId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InspectionSchedules"
    ADD CONSTRAINT "FK_InspectionSchedules_BookingReports_ReportId" FOREIGN KEY ("ReportId") REFERENCES public."BookingReports"("Id");


--
-- Name: InspectionSchedules FK_InspectionSchedules_CarReports_CarReportId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InspectionSchedules"
    ADD CONSTRAINT "FK_InspectionSchedules_CarReports_CarReportId" FOREIGN KEY ("CarReportId") REFERENCES public."CarReports"("Id");


--
-- Name: InspectionSchedules FK_InspectionSchedules_Cars_CarId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InspectionSchedules"
    ADD CONSTRAINT "FK_InspectionSchedules_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES public."Cars"("Id") ON DELETE CASCADE;


--
-- Name: InspectionSchedules FK_InspectionSchedules_Users_CreatedBy; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InspectionSchedules"
    ADD CONSTRAINT "FK_InspectionSchedules_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: InspectionSchedules FK_InspectionSchedules_Users_TechnicianId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InspectionSchedules"
    ADD CONSTRAINT "FK_InspectionSchedules_Users_TechnicianId" FOREIGN KEY ("TechnicianId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Models FK_Models_Manufacturers_ManufacturerId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Models"
    ADD CONSTRAINT "FK_Models_Manufacturers_ManufacturerId" FOREIGN KEY ("ManufacturerId") REFERENCES public."Manufacturers"("Id") ON DELETE CASCADE;


--
-- Name: RefreshTokens FK_RefreshTokens_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Transactions FK_Transactions_BankAccounts_BankAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "FK_Transactions_BankAccounts_BankAccountId" FOREIGN KEY ("BankAccountId") REFERENCES public."BankAccounts"("Id");


--
-- Name: Transactions FK_Transactions_Bookings_BookingId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "FK_Transactions_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES public."Bookings"("Id");


--
-- Name: Transactions FK_Transactions_TransactionTypes_TypeId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "FK_Transactions_TransactionTypes_TypeId" FOREIGN KEY ("TypeId") REFERENCES public."TransactionTypes"("Id") ON DELETE CASCADE;


--
-- Name: Transactions FK_Transactions_Users_FromUserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "FK_Transactions_Users_FromUserId" FOREIGN KEY ("FromUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Transactions FK_Transactions_Users_ToUserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "FK_Transactions_Users_ToUserId" FOREIGN KEY ("ToUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: TripTrackings FK_TripTrackings_Bookings_BookingId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."TripTrackings"
    ADD CONSTRAINT "FK_TripTrackings_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES public."Bookings"("Id") ON DELETE CASCADE;


--
-- Name: UserStatistics FK_UserStatistics_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."UserStatistics"
    ADD CONSTRAINT "FK_UserStatistics_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Users FK_Users_EncryptionKeys_EncryptionKeyId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "FK_Users_EncryptionKeys_EncryptionKeyId" FOREIGN KEY ("EncryptionKeyId") REFERENCES public."EncryptionKeys"("Id") ON DELETE CASCADE;


--
-- Name: Users FK_Users_UserRoles_RoleId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "FK_Users_UserRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."UserRoles"("Id") ON DELETE CASCADE;


--
-- Name: WithdrawalRequests FK_WithdrawalRequests_BankAccounts_BankAccountId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."WithdrawalRequests"
    ADD CONSTRAINT "FK_WithdrawalRequests_BankAccounts_BankAccountId" FOREIGN KEY ("BankAccountId") REFERENCES public."BankAccounts"("Id") ON DELETE CASCADE;


--
-- Name: WithdrawalRequests FK_WithdrawalRequests_Transactions_TransactionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."WithdrawalRequests"
    ADD CONSTRAINT "FK_WithdrawalRequests_Transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES public."Transactions"("Id");


--
-- Name: WithdrawalRequests FK_WithdrawalRequests_Users_ProcessedByAdminId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."WithdrawalRequests"
    ADD CONSTRAINT "FK_WithdrawalRequests_Users_ProcessedByAdminId" FOREIGN KEY ("ProcessedByAdminId") REFERENCES public."Users"("Id");


--
-- Name: WithdrawalRequests FK_WithdrawalRequests_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."WithdrawalRequests"
    ADD CONSTRAINT "FK_WithdrawalRequests_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

