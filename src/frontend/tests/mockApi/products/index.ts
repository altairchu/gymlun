import { db } from '../db';
import data from './products.data.json';
import { clearPhotos } from './products.service';

export { handlers as productsHandlers } from './products.handlers';

export const fillProducts = (): void => {
  clearPhotos();
  data.forEach(product => {
    db.product.create(product);
  });
};
