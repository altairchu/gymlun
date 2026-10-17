# Food Diary

A self-hosted nutrition and weight tracker for one person. This glossary fixes the
vocabulary used across the backend, the frontend, and the MCP contract.

## Language

**Food Log**:
One product eaten in a given amount, at one time of day, on one date — never a whole meal
or a whole day. The canonical name wherever a person or a language model reads it: the UI,
user documentation, and the MCP contract.
_Avoid_: meal log, entry, serving, portion

**Note**:
The legacy name for a Food Log. It remains in the database, the domain entities, the
Application handlers, and older parts of the UI; new UI text says food log.
_Avoid_: memo, comment (a Note carries no text)

**Time of Day**:
When a Food Log was eaten, to the minute, between 00:00 and 23:59. The diary lays a day's
Food Logs out on a timeline by it, and a Food Log can be moved to another time.
_Avoid_: timestamp, slot

**Meal**:
One of five fixed slots in a day — breakfast, second breakfast, lunch, afternoon snack,
dinner. Each Food Log's meal follows from its time of day; a meal without a time stands
for that meal's usual time.
_Avoid_: mealtime, eating occasion

**Product**:
An eatable product or dish in the personal database, holding nutrition values per 100 g.
A Product is the catalogue item; a Food Log is one occasion of eating some of it.
_Avoid_: ingredient

**Favorite**:
A Product the person eats often, marked so it shows under the diary's favourites filter
and is found first by an MCP client.
_Avoid_: pinned, starred, frequent

**Nutrition Values**:
Calories in kilocalories, and protein, fats, carbs, sugar and salt in grams. Salt means
sodium chloride, never sodium. On a Product they are per 100 g; on a Food Log they are scaled
to the quantity eaten. Quantities are stored in grams, drinks included — there is no
millilitre.
_Avoid_: joules, kJ, sodium, ml

**Quantity Unit**:
How a Product is counted when it is logged: by weight in grams, or in servings (pieces,
bowls) of its default quantity in grams. Set per Product, and can be switched for one Food
Log while logging it; a Food Log always stores grams.
_Avoid_: measure, portion type

**Category**:
A grouping of Products in the personal database. A Product can be in several Categories,
or in none.

**Weight Log**:
The weight recorded for one date. At most one per date.
_Avoid_: weigh-in, measurement
