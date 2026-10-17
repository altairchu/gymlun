import { createFileRoute } from '@tanstack/react-router';
import { productApi, productLib } from '@/entities/product';
import { ProductsPage } from '@/pages/ui/ProductsPage';

export const Route = createFileRoute('/_app/products')({
  staticData: {
    appBar: { variant: 'menu', title: '食物' },
  },
  loader: async ({ context }) => {
    const request = productLib.mapToGetProductsRequest(context.store.getState().products.filter);
    const productsQueryPromise = context.store.dispatch(
      productApi.endpoints.getProducts.initiate(request),
    );

    try {
      await productsQueryPromise;
    } finally {
      productsQueryPromise.unsubscribe();
    }
  },
  component: ProductsPage,
});
