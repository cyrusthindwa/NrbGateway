-- Update the existing individual with proper demographic data
UPDATE kyc.individuals SET
    "Title" = 'MR',
    "Surname" = 'BANDA',
    "FirstName" = 'PETER',
    "OtherNames" = 'JOHN',
    "DateOfBirth" = '1985-07-15',
    "PlaceOfBirthVillage" = 'KAWALE',
    "PlaceOfBirthDistrict" = 'LILONGWE',
    "Gender" = 'MALE',
    "CivilStatus" = 'MARRIED',
    "Nationality" = 'MALAWI',
    "UpdatedAt" = NOW()
WHERE "NationalIdHash" = (
    SELECT "NationalIdHash" FROM kyc.individuals 
    WHERE "FirstName" = 'PENDING_VERIFICATION' 
    LIMIT 1
);

-- Show the updated record
SELECT "Id", "Surname", "FirstName", "OtherNames", "DateOfBirth", 
       "PlaceOfBirthDistrict", "Gender", "Nationality", "RecordStatus"
FROM kyc.individuals
WHERE "FirstName" = 'PETER';
