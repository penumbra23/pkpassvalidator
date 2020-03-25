# What does this do?
Checks the validity of a pkpass file by checking its signature and contents match the Apple specification. It's hosted on Azure (at my own expense) but I wanted to post the code here, so people can a) see what it does and b) can contribute to the validation it performs.

## API extensions
This project extends the pkpassvalidator repo with an API support. The only endpoint is the `/api/validation` which takes a base64 encoded file and returns the pkpass checks.

Example:
```
POST http://localhost:8080/api/validation
{
	"encoded_bytes": "UEsDBAoAAAAAABsIREIRX4..."
}
```

```
{
    "data": {
        "has_pass": true,
        "has_manifest": true,
        "has_signature": true,
        "team_identifier_matches": true,
        "pass_type_identifier_matches": true,
        "signed_by_apple": true,
        "has_signature_expired": true,
        "signature_expiration_date": "2014-01-28 01:07:31",
        "has_icon3x": false,
        "has_icon2x": true,
        "has_icon1x": true,
        "has_pass_type_identifier": true,
        "has_team_identifier": true,
        "has_description": true,
        "has_format_version": true,
        "has_serial_number": true,
        "has_serial_number_of_correct_length": false,
        "has_organization_name": true,
        "has_app_launch_url": false,
        "has_associated_store_identifiers": false,
        "wwdr_certificate_expired": true,
        "wwdr_certificate_subject_matches": false,
        "has_authentication_token": false,
        "authentication_token_is_correct_length": false,
        "has_web_service_url": false,
        "web_service_url_is_https": false
    }
}
```

Everything else is the same as in the original repository.

## Motivation
Questions pop up on StackOverflow about invalid passes and the cause, usually, is a problem in the payload. This project represents my attempt to help developer diagnose the issues themselves. 

## Where is it hosted?
The project is available at https://pkpassvalidator.azurewebsites.net and can be used right now. I'll extend its capabilities over time.

## Support the project
If you find this utility useful, please consider donating by buying Tom a coffee - https://www.buymeacoffee.com/fMKJ2NnQ3

