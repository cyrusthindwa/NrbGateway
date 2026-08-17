UPDATE kyc.individuals SET
    "OtherNames" = 'JOHN',
    "PlaceOfBirthDistrict" = 'LILONGWE',
    "PlaceOfBirthVillage" = 'KAWALE',
    "DateOfBirth" = '1985-07-15',
    "Nationality" = 'MALAWI',
    "CivilStatus" = 'MARRIED'
WHERE "FirstName" = 'PETER' AND "Surname" = 'BANDA';

SELECT "Surname", "FirstName", "OtherNames", "DateOfBirth", "PlaceOfBirthDistrict", "Nationality", "RecordStatus"
FROM kyc.individuals WHERE "FirstName" = 'PETER';
