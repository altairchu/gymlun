import { toPer100g, toPerServing } from './servings';

describe('servings', () => {
  it('converts a serving of dumplings to values per 100 g', () => {
    const per100g = toPer100g(
      { calories: 550, protein: 22.5, fats: 20, carbs: 70, sugar: null, salt: 3 },
      250,
    );

    expect(per100g).toEqual({
      calories: 220,
      protein: 9,
      fats: 8,
      carbs: 28,
      sugar: null,
      salt: 1.2,
    });
  });

  it('converts values per 100 g to a serving', () => {
    const perServing = toPerServing(
      { calories: 220, protein: 9, fats: 8, carbs: 28, sugar: null, salt: 1.2 },
      250,
    );

    expect(perServing).toEqual({
      calories: 550,
      protein: 22.5,
      fats: 20,
      carbs: 70,
      sugar: null,
      salt: 3,
    });
  });
});
