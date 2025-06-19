import base64
import json

# JWT token from your request
token = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJjNGY1Y2I2MS00MzM1LTQ1OWUtOTk2OC01NmNkZDRmOGM1ZGMiLCJuYW1lIjoiZGlhbmVrYXJhaGFyam8iLCJlbWFpbCI6ImRpYW5la2FyYWhhcmpvQGdtYWlsLmNvbSIsImp0aSI6IjY2MDA0YTk0LTU2ZmQtNGUxNS1hNGE0LTk0YmE3MGJmOTY1NCIsIm5iZiI6MTc1MDM0MzkxMiwiZXhwIjoxNzUwOTQ4NzEyLCJpYXQiOjE3NTAzNDM5MTJ9.DdyOeTB0tESOEd6Jw0RY4cNYupzlh7u36m8Qt93iniDBi8d7mjYLXejG0IhDoNZ1TXUXUlqYEC3eStts2dp1QQ"

# Split the token
parts = token.split('.')
header = parts[0]
payload = parts[1]
signature = parts[2]

# Decode header
header_decoded = base64.urlsafe_b64decode(header + '==')
print("Header:")
print(json.dumps(json.loads(header_decoded), indent=2))

# Decode payload
payload_decoded = base64.urlsafe_b64decode(payload + '==')
print("\nPayload:")
payload_json = json.loads(payload_decoded)
print(json.dumps(payload_json, indent=2))

# Check expiration
import datetime
exp_timestamp = payload_json['exp']
exp_date = datetime.datetime.fromtimestamp(exp_timestamp)
now = datetime.datetime.now()

print(f"\nToken expires at: {exp_date}")
print(f"Current time: {now}")
print(f"Token is {'EXPIRED' if now > exp_date else 'VALID'}")
